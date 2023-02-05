/********************************************************************************
* ObjectPool.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Solti.Utils.Primitives.Threading
{
    using Patterns;
    using Properties;

    /// <summary>
    /// Represents a requested pool item.
    /// </summary>
    public class PoolItem<T> : Disposable, IWrapped<T> where T : class
    {
        /// <summary>
        /// Creates a new <see cref="PoolItem{T}"/> instance.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="value"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public PoolItem(ObjectPool<T> owner, T value)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// The owner of this item.
        /// </summary>
        public ObjectPool<T> Owner { get; }

        /// <summary>
        /// The value of this item.
        /// </summary>
        public T Value { get; }

        /// <inheritdoc/>
        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
                Owner.Return(Value);

            base.Dispose(disposeManaged);
        }
    }

    /// <summary>
    /// Defines some extensions for the <see cref="ObjectPool{T}"/> class.
    /// </summary>
    public static class ObjectPoolExtensions
    {
        /// <summary>
        /// Gets an item from the pool.
        /// </summary>
        public static PoolItem<T>? GetItem<T>(this ObjectPool<T> self, CancellationToken cancellation = default) where T : class
        {
            Ensure.Parameter.IsNotNull(self, nameof(self));

            T? value = self.Get(cancellation);

            return value is null
                ? null
                : new PoolItem<T>(self, value);
        }
    }

    /// <summary>
    /// Describes the <see cref="ObjectPool{T}.Get(CancellationToken)"/> behavior when the request can not be granted.
    /// </summary>
    public enum CheckoutPolicy 
    {
        /// <summary>
        /// The calling thread is blocked until the request can be served.
        /// </summary>
        Block,

        /// <summary>
        /// An <see cref="InvalidOperationException"/> is thrown.
        /// </summary>
        Throw,

        /// <summary>
        /// The pool discards the request and returns NULL.
        /// </summary>
        Discard
    }

    /// <summary>
    /// Describes how to manage the lifetime of pool items.
    /// </summary>
    public interface ILifetimeManager<T> where T : class
    {
        /// <summary>
        /// Creates a new pool item.
        /// </summary>
        T Create();

        /// <summary>
        /// Prepares the item to be checked out.
        /// </summary>
        void CheckOut(T item);

        /// <summary>
        /// Resets the state of the item.
        /// </summary>
        void CheckIn(T item);

        /// <summary>
        /// Disposes a pool item.
        /// </summary>
        void Dispose(T item);

        /// <summary>
        /// The default lifetime manager
        /// </summary>
        [SuppressMessage("Naming", "CA1716:Identifiers should not match keywords")]
        public sealed class Default : ILifetimeManager<T>
        {
            /// <summary>
            /// The associated instance factory.
            /// </summary>
            public Func<T> Factory { get; }

            /// <summary>
            /// Creates a new instance from the default lifetime manager.
            /// </summary>
            public Default(Func<T> factory) => Factory = factory ?? throw new ArgumentNullException(nameof(factory));

            /// <inheritdoc/>
            public T Create() => Factory();

            /// <inheritdoc/>
            public void Dispose(T item)
            {
                if (item is IDisposable disposable)
                    disposable.Dispose();
            }

            /// <inheritdoc/>
            public void CheckOut(T item) { }

            /// <inheritdoc/>
            public void CheckIn(T item)
            {
                if (item is IResettable resettable && resettable.Dirty)
                {
                    resettable.Reset();

                    if (resettable.Dirty)
                        throw new InvalidOperationException(Resources.RESET_FAILED);
                }
            }
        }
    }

    /// <summary>
    /// Poll configuration.
    /// </summary>
    public record PoolConfig
    {
        /// <summary>
        /// Describes how to checkout items from the pool.
        /// </summary>
        public CheckoutPolicy CheckoutPolicy { get; init; } = CheckoutPolicy.Block;

        /// <summary>
        /// Specifies whether the pool checkout ios permissive or not.
        /// </summary>
        /// <remarks>Permissive pool let the same thread checkout multiple items from the pool simultaneously.</remarks>
        public bool Permissive { get; init; }

        /// <summary>
        /// The maximum capacity.
        /// </summary>
        public int Capacity { get; init; } = Environment.ProcessorCount;

        /// <summary>
        /// The default value.
        /// </summary>
        public static PoolConfig Default { get; } = new();
    }

    /// <summary>
    /// Describes a simple object pool.
    /// </summary>
    public class ObjectPool<T>: Disposable, IReadOnlyCollection<T> where T: class
    {
        private readonly ConcurrentBag<T?> FUnusedItems = new();

        private readonly ConcurrentBag<T> FCreatedItems = new();

        private readonly ThreadLocal<T?>? FCurrentItem;

        /// <summary>
        /// Creates a new <see cref="ObjectPool{T}"/> object.
        /// </summary>
        public ObjectPool(ILifetimeManager<T> lifetimeManager, PoolConfig? config = null)
        {
            Config = config ?? PoolConfig.Default;

            if (!Config.Permissive)
                FCurrentItem = new ThreadLocal<T?>(trackAllValues: false);

            for (int i = 0; i < Config.Capacity; i++)
            {
                FUnusedItems.Add(null);
            }

            LifetimeManager = Ensure.Parameter.IsNotNull(lifetimeManager, nameof(lifetimeManager));     
        }

        /// <summary>
        /// Creates a new <see cref="ObjectPool{T}"/> object.
        /// </summary>
        public ObjectPool(Func<T> factory, PoolConfig? config = null) : this
        (
            new ILifetimeManager<T>.Default
            (
                Ensure.Parameter.IsNotNull(factory, nameof(factory))
            ),
            config
        ) { }

        /// <summary>
        /// The pool configuration.
        /// </summary>
        public PoolConfig Config { get; }

        /// <summary>
        /// The <see cref="ILifetimeManager{T}"/> of the items exposed by this pool.
        /// </summary>
        public ILifetimeManager<T> LifetimeManager { get; }

        /// <inheritdoc/>
        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                foreach (T item in this)
                {
                    try
                    {
                        LifetimeManager.Dispose(item);
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine($"Can't dispose pool item: {e}");
                    }
                }
                FCurrentItem?.Dispose();
            }

            base.Dispose(disposeManaged);
        }

        /// <summary>
        /// Gets a value from the pool.
        /// </summary>
        public T? Get(CancellationToken cancellation = default)
        {
            CheckNotDisposed();

            SpinWait? spinWait = null;

            //
            // Check if the requesting thread already has an instance
            //

            if (FCurrentItem?.Value is not null)
                return FCurrentItem.Value;

            //
            // Nope... Grab one...
            //

            do
            {
                cancellation.ThrowIfCancellationRequested();

                if (FUnusedItems.TryTake(out T? item))
                {
                    try
                    {
                        //
                        // Create the value (if it hasn't been...)
                        //

                        if (item is null)
                        {
                            item = LifetimeManager.Create();
                            FCreatedItems.Add(item);
                        }

                        LifetimeManager.CheckOut(item);
                    }
                    catch
                    {
                        //
                        // Revert the slot back to "unused" if there was an error.
                        //

                        FUnusedItems.Add(item);
                        throw;
                    }

                    if (FCurrentItem is not null)
                        FCurrentItem.Value = item;

                    return item;
                }

                switch (Config.CheckoutPolicy)
                {
                    case CheckoutPolicy.Block:
                        break;
                    case CheckoutPolicy.Throw:
                        throw new InvalidOperationException(string.Format(Resources.Culture, Resources.MAX_SIZE_REACHED, Config.Capacity));
                    case CheckoutPolicy.Discard:
                        return default;
                }

                (spinWait ??= new()).SpinOnce();
            } while (true);
        }

        /// <summary>
        /// Returns the item to the pool, identified by its index.
        /// </summary>
        public void Return(T item) 
        {
            Ensure.Parameter.IsNotNull(item, nameof(item));

            CheckNotDisposed();

            if (FCurrentItem is not null)
            {
                if (FCurrentItem.Value != item)
                    throw new InvalidOperationException(Resources.RETURN_NOT_ALLOWED);
                FCurrentItem.Value = null;
            }

            LifetimeManager.CheckIn(item);
            FUnusedItems.Add(item);
        }

        /// <summary>
        /// Enumerates the already produced items.
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            CheckNotDisposed();
            return FCreatedItems.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// The count of crested objects.
        /// </summary>
        public int Count
        {
            get 
            {
                CheckNotDisposed();
                return FCreatedItems.Count;
            }
        }
    }
}
