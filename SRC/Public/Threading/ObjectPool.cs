/********************************************************************************
* ObjectPool.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
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
    /// Represents a requested pool item.
    /// </summary>
    #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public class PoolItem<T> : Disposable, IWrapped<T> where T : class
    {
        /// <summary>
        /// The owner of this item.
        /// </summary>
        public ObjectPool<T> Owner { get; init; }

        /// <summary>
        /// The value of this item.
        /// </summary>
        public T Value { get; init; }

        /// <inheritdoc/>
        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
                Owner.Return(Value);

            base.Dispose(disposeManaged);
        }
    }
    #pragma warning restore CS8618

    /// <summary>
    /// Defines some extensions for the <see cref="ObjectPool{T}"/> class.
    /// </summary>
    public static class ObjectPoolExtensions
    {
        /// <summary>
        /// Gets an item from the pool.
        /// </summary>
        public static PoolItem<T>? GetItem<T>(this ObjectPool<T> self, CheckoutPolicy checkoutPolicy = CheckoutPolicy.Block, CancellationToken cancellation = default) where T : class
        {
            Ensure.Parameter.IsNotNull(self, nameof(self));

            T? value = self.Get(checkoutPolicy, cancellation);

            return value is null
                ? null
                : new PoolItem<T>
                {
                    Value = value,
                    Owner = self
                };
        }
    }

    /// <summary>
    /// Describes the <see cref="ObjectPool{T}.Get(CheckoutPolicy, CancellationToken)"/> behavior when the request can not be granted.
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
    public interface ILifetimeManager<T>
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
        /// Defines a handler that is called when recursive factory is detected. 
        /// </summary>
        void RecursionDetected();

        /// <summary>
        /// Disposes a pool item.
        /// </summary>
        void Dispose(T item);


        /// <summary>
        /// The default lifetime manager
        /// </summary>
        [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
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

            /// <inheritdoc/>
            public void RecursionDetected() => throw new InvalidOperationException(Resources.RECURSIVE_FACTORY);
        }
    }

    /// <summary>
    /// Describes a simple object pool.
    /// </summary>
    public class ObjectPool<T>: Disposable, IReadOnlyCollection<(int OwnerThread, T Object)> where T: class
    {
        private readonly (int OwnerThread, T? Object)[] FObjects;

        /// <summary>
        /// Creates a new <see cref="ObjectPool{T}"/> object.
        /// </summary>
        public ObjectPool(ILifetimeManager<T> lifetimeManager, int? maxPoolSize = null)
        {
            Capacity = maxPoolSize ?? Environment.ProcessorCount;
            FObjects = new (int OwnerThread, T? Object)[Capacity]; // mivel Tuple-k ertek tipusok ezert nincs gond az inicialassal
            LifetimeManager = Ensure.Parameter.IsNotNull(lifetimeManager, nameof(lifetimeManager));     
        }

        /// <summary>
        /// Creates a new <see cref="ObjectPool{T}"/> object.
        /// </summary>
        public ObjectPool(Func<T> factory, int? maxPoolSize = null) : this
        (
            new ILifetimeManager<T>.Default
            (
                Ensure.Parameter.IsNotNull(factory, nameof(factory))
            ),
            maxPoolSize
        ) { }

        /// <summary>
        /// The maximum number of objects that can be checked out in the same time.
        /// </summary>
        public int Capacity { get; }

        /// <summary>
        /// The <see cref="ILifetimeManager{T}"/> of the items exposed by this pool.
        /// </summary>
        public ILifetimeManager<T> LifetimeManager { get; }

        /// <inheritdoc/>
        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                foreach ((int _, T Object) item in this)
                {
                    try
                    {
                        LifetimeManager.Dispose(item.Object);
                    }
                    #pragma warning disable CA1031 // This method should not throw.
                    catch (Exception e)
                    #pragma warning restore CA1031
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
        public T? Get(CheckoutPolicy checkoutPolicy = CheckoutPolicy.Block, CancellationToken cancellation = default)
        {
            CheckNotDisposed();

            SpinWait? spinWait = null;

            int currentThread = Environment.CurrentManagedThreadId;

            do
            {
                cancellation.ThrowIfCancellationRequested();

                //
                // Megprobalunk szabad helyet keresni a pool-ban
                //

                for (int i = 0; i < Capacity; i++)
                {
                    ref (int OwnerThread, T? Object) holder = ref FObjects[i]; // nem masolat

                    if (holder.OwnerThread == currentThread && holder.Object is null)
                        //
                        // Object lehet NULL ha a factory maga is elemet akarna kivenni a pool-bol
                        //

                        LifetimeManager.RecursionDetected();

                    //
                    // Az elso olyan elem ami meg nincs kicsekkolva
                    //

                    if (Interlocked.CompareExchange(ref holder.OwnerThread, currentThread, 0) == 0)
                    {
                        try
                        {
                            //
                            // Lehet h meg nem is vt legyartva hozza elem, akkor legyartjuk
                            //

                            T result = (holder.Object ??= LifetimeManager.Create());

                            LifetimeManager.CheckOut(holder.Object);

                            return result;
                        }
                        catch
                        {
                            //
                            // Ha hiba vt a factory-ban akkor az elem ne maradjon kicsekkolva.
                            //

                            Interlocked.Exchange(ref holder.OwnerThread, 0);
                            throw;
                        }
                    }
                }

                switch (checkoutPolicy)
                {
                    case CheckoutPolicy.Block:
                        break;
                    case CheckoutPolicy.Throw:
                        throw new InvalidOperationException(string.Format(Resources.Culture, Resources.MAX_SIZE_REACHED, Capacity));
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

            int currentThread = Environment.CurrentManagedThreadId;

            for (int i = 0; i < Capacity; i++)
            {
                ref (int OwnerThread, T? Object) holder = ref FObjects[i]; // nem masolat

                if (holder.Object == item)
                {
                    if (holder.OwnerThread != currentThread)
                        Trace.WriteLine("Returning item from a different thread");

                    LifetimeManager.CheckIn(holder.Object!);
                    Interlocked.Exchange(ref holder.OwnerThread, 0);

                    break;
                }
            }
        }

        /// <summary>
        /// Enumerates the already produced items.
        /// </summary>
        public IEnumerator<(int OwnerThread, T Object)> GetEnumerator()
        {
            CheckNotDisposed();

            for (int i = 0; i < Capacity; i++)
            {
                //
                // Masolat, ami nekunk jo is mert igy a visszaadott elem szabadon modosithato
                //

                (int OwnerThread, T? Object) holder = FObjects[i];

                //
                // Mivel a Get() mindig a legelso meg ures tarolot tolti fel, ezert ha ures tarolohoz
                // ertunk nem kell folytatni a felsorolast.
                //

                if (holder.Object is null)
                    yield break;
                                    
                yield return holder!;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// The count of checked out objects.
        /// </summary>
        public int Count => this.Count(entry => entry.OwnerThread is not 0);
    }
}
