/********************************************************************************
* ObjectPool.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
        public static PoolItem<T>? GetItem<T>(this ObjectPool<T> self, CheckoutPolicy checkoutPolicy = CheckoutPolicy.Block, CancellationToken cancellation = default) where T: class
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
    }

    /// <summary>
    /// Describes a simple object pool.
    /// </summary>
    public class ObjectPool<T>: Disposable, IReadOnlyCollection<(int OwnerThread, T Object)> where T: class
    {
        private readonly (int OwnerThread, T? Object)[] FObjects;

        private sealed class DefaultLifetimeManager : ILifetimeManager<T>
        {
            public Func<T> Factory { get; }

            public DefaultLifetimeManager(Func<T> factory)
            {
                Factory = factory;
            }

            public T Create() => Factory();

            public void Dispose(T item)
            {
                if (item is IDisposable disposable)
                    disposable.Dispose();
            }

            public void CheckOut(T item) { }

            public void CheckIn(T item)
            {
                if (item is IResettable resettable && resettable.Dirty)
                {
                    resettable.Reset();

                    if (resettable.Dirty)
                        throw new InvalidOperationException(Resources.RESET_FAILED);
                }
            }

            public void RecursionDetected() => throw new InvalidOperationException(Resources.RECURSION_NOT_ALLOWED);
        }

        /// <summary>
        /// Creates a new <see cref="ObjectPool{T}"/> object.
        /// </summary>
        public ObjectPool(int maxPoolSize, ILifetimeManager<T> lifetimeManager)
        {
            FObjects = new (int OwnerThread, T? Object)[maxPoolSize]; // mivel Tuple-k ertek tipusok ezert nincs gond az inicialassal

            LifetimeManager = Ensure.Parameter.IsNotNull(lifetimeManager, nameof(lifetimeManager));
            Capacity = maxPoolSize;
        }

        /// <summary>
        /// Creates a new <see cref="ObjectPool{T}"/> object.
        /// </summary>
        public ObjectPool(int maxPoolSize, Func<T> factory): this(maxPoolSize, new DefaultLifetimeManager(Ensure.Parameter.IsNotNull(factory, nameof(factory)))) 
        {
        }

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

            T? result = null;

            SpinWait.SpinUntil(() =>
            {
                cancellation.ThrowIfCancellationRequested();

                //
                // Megprobalunk szabad helyet keresni a pool-ban
                //

                for (int i = 0; i < Capacity; i++)
                {
                    ref (int OwnerThread, T? Object) holder = ref FObjects[i]; // nem masolat

                    if (holder.OwnerThread == Thread.CurrentThread.ManagedThreadId && holder.Object is null)
                        //
                        // Object lehet NULL ha a factory maga is elemet akarna kivenni a pool-bol
                        //

                        LifetimeManager.RecursionDetected();

                    //
                    // Az elso olyan elem ami meg nincs kicsekkolva
                    //

                    if (Interlocked.CompareExchange(ref holder.OwnerThread, Thread.CurrentThread.ManagedThreadId, 0) == 0)
                    {
                        try
                        {
                            //
                            // Lehet h meg nem is vt legyartva hozza elem, akkor legyartjuk
                            //

                            result = (holder.Object ??= LifetimeManager.Create());

                            LifetimeManager.CheckOut(holder.Object);

                            return true;
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

                //
                // Nem volt szabad hely a poolban...
                //

                if (checkoutPolicy is CheckoutPolicy.Throw)
                    throw new InvalidOperationException(string.Format(Resources.Culture, Resources.MAX_SIZE_REACHED, Capacity));

                //
                // Ha a checkoutPolicy == CheckoutPolicy.Block akkor a ciklus ujra kezdodik.
                //

                return checkoutPolicy is CheckoutPolicy.Discard;
            });

            return result;
        }

        /// <summary>
        /// Returns the item to the pool, identified by its index.
        /// </summary>
        public void Return(T item) 
        {
            Ensure.Parameter.IsNotNull(item, nameof(item));

            CheckNotDisposed();

            for (int i = 0; i < Capacity; i++)
            {
                ref (int OwnerThread, T? Object) holder = ref FObjects[i]; // nem masolat

                if (holder.Object == item)
                {
                    if (holder.OwnerThread != Thread.CurrentThread.ManagedThreadId)
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
