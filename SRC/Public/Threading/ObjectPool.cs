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
using System.Linq;
using System.Threading;

namespace Solti.Utils.Primitives.Threading
{
    using Patterns;
    using Properties;

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
            public void Dispose(T item) => (item as IDisposable)?.Dispose();

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
        /// The maximum capacity.
        /// </summary>
        public int Capacity { get; init; } = Environment.ProcessorCount;

        /// <summary>
        /// The default value.
        /// </summary>
        public static PoolConfig Default { get; } = new();
    }

    /// <summary>
    /// Describes an abstract pool item.
    /// </summary>
    /// <remarks>Disposing this instance will return return the item to the pool.</remarks>
    public interface IPoolItem<T> : IDisposable where T : class
    {
        /// <summary>
        /// The value itself.
        /// </summary>
        T Value { get; }
    }

    /// <summary>
    /// Describes a simple object pool.
    /// </summary>
    public class ObjectPool<T>: Disposable, IReadOnlyCollection<T> where T: class
    {
        private sealed class PoolItem: IPoolItem<T>
        {
            public PoolItem(ObjectPool<T> owner, PoolItem? prev)
            {
                Owner = owner;
                Prev = prev;
                Value = null!;  // suppress warning
            }

            public PoolItem? Prev { get; }

            public ObjectPool<T> Owner { get; }

            public T Value { get; private set; }

            public bool CheckedOut { get; private set; }

            public void Checkout()
            {
                Value ??= Owner.LifetimeManager.Create();
                Owner.LifetimeManager.CheckOut(Value);
                CheckedOut = true;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                if (CheckedOut)
                {
                    if (Value is not null)
                        Owner.LifetimeManager.CheckIn(Value);
                    Owner.FUnusedItems.Add(this);
                }
            }
        }

        private readonly ConcurrentBag<PoolItem> FUnusedItems = new();

        private PoolItem? Last { get; }

        /// <summary>
        /// Creates a new <see cref="ObjectPool{T}"/> object.
        /// </summary>
        public ObjectPool(ILifetimeManager<T> lifetimeManager, PoolConfig? config = null)
        {
            Config = config ?? PoolConfig.Default;
            LifetimeManager = Ensure.Parameter.IsNotNull(lifetimeManager, nameof(lifetimeManager));

            for (int i = 0; i < Config.Capacity; i++)
            {
                PoolItem item = new(this, Last);
                Last = item;
                FUnusedItems.Add(item);
            }   
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
            }

            base.Dispose(disposeManaged);
        }

        /// <summary>
        /// Gets a value from the pool.
        /// </summary>
        public IPoolItem<T>? Get(CancellationToken cancellation = default)
        {
            CheckNotDisposed();

            SpinWait? spinWait = null;

            do
            {
                cancellation.ThrowIfCancellationRequested();

                if (FUnusedItems.TryTake(out PoolItem item))
                {
                    try
                    {
                        item.Checkout();
                    }
                    catch
                    {
                        //
                        // Revert the slot back to "unused" if there was an error.
                        //

                        FUnusedItems.Add(item);
                        throw;
                    }

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
        /// Enumerates the already produced items.
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            CheckNotDisposed();
            return GetEnumerable().GetEnumerator();
            
            IEnumerable<T> GetEnumerable()
            {
                PoolItem? item = Last;
                while (item is not null)
                {
                    if (item.Value is not null)
                        yield return item.Value;
                    item = item.Prev;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// The count of created objects.
        /// </summary>
        public int Count
        {
            get 
            {
                CheckNotDisposed();
                return this.Count();
            }
        }
    }
}
