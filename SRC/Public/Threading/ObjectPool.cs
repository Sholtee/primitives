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
                Owner.Return();

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
            if (self is null)
                throw new ArgumentNullException(nameof(self));

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
        /// Resets the state of a pool item.
        /// </summary>
        /// <param name="item"></param>
        void Reset(T item);

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

        private readonly ThreadLocal<GetObjectHolder?> FHeldObject;

        private delegate ref (int OwnerThread, T? Object) GetObjectHolder();

        private sealed class DefaultLifetimeManager : ILifetimeManager<T>
        {
            public Func<T> Factory { get; }

            public bool SuppressItemDispose { get; }

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

            public void Reset(T item)
            {
                if (item is IResettable resettable && resettable.Dirty)
                {
                    resettable.Reset();

                    if (resettable.Dirty)
                        throw new InvalidOperationException(Resources.RESET_FAILED);
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="ObjectPool{T}"/> object.
        /// </summary>
        public ObjectPool(int maxPoolSize, ILifetimeManager<T> lifetimeManager)
        {
            FObjects = new (int OwnerThread, T? Object)[maxPoolSize]; // mivel Tuple-k ertek tipusok ezert nincs gond az inicialassal
            FHeldObject = new ThreadLocal<GetObjectHolder?>(trackAllValues: false);

            LifetimeManager = lifetimeManager ?? throw new ArgumentNullException(nameof(lifetimeManager));
            Capacity = maxPoolSize;
        }

        /// <summary>
        /// Creates a new <see cref="ObjectPool{T}"/> object.
        /// </summary>
        public ObjectPool(int maxPoolSize, Func<T> factory): this(maxPoolSize, new DefaultLifetimeManager(factory ?? throw new ArgumentNullException(nameof(factory)))) 
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

                FHeldObject.Dispose();
            }

            base.Dispose(disposeManaged);
        }

        /// <summary>
        /// Gets a value from the pool.
        /// </summary>
        public T? Get(CheckoutPolicy checkoutPolicy = CheckoutPolicy.Block, CancellationToken cancellation = default)
        {
            CheckNotDisposed();

            //
            // Szalankent csak egyszer vehetunk ki a pool-bol elemet
            //

            if (FHeldObject.Value is not null)
                //
                // FHeldObject.Value().Object lehet NULL ha a factory maga is elemet akarna kivenni a pool-bol
                //

                return FHeldObject.Value().Object ?? throw new InvalidOperationException(Resources.RECURSION_NOT_ALLOWED);

            SpinWait.SpinUntil(() =>
            {
                cancellation.ThrowIfCancellationRequested();

                //
                // Megprobalunk szabad helyet keresni a pool-ban
                //

                for (int i = 0; i < Capacity; i++)
                {
                    ref (int OwnerThread, T? Object) holder = ref FObjects[i]; // nem masolat

                    //
                    // Az elso olyan elem ami meg nincs kicsekkolva
                    //

                    if (Interlocked.CompareExchange(ref holder.OwnerThread, Thread.CurrentThread.ManagedThreadId, 0) == 0)
                    {
                        FHeldObject.Value = () => ref FObjects[i];

                        try
                        {
                            //
                            // Lehet h meg nem is vt legyartva hozza elem, akkor legyartjuk
                            //

                            holder.Object ??= LifetimeManager.Create();
                            return true;
                        }
                        catch
                        {
                            //
                            // Ha hiba vt a factory-ban akkor az elem ne maradjon kicsekkolva.
                            //

                            Return();
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

            return FHeldObject.Value?.Invoke().Object;
        }

        /// <summary>
        /// Returns the item to the pool, identified by its index.
        /// </summary>
        public void Return() 
        {
            CheckNotDisposed();

            if (FHeldObject.Value is null)
                return;

            ref (int OwnerThread, T? Object) holder = ref FHeldObject.Value(); // nem masolat

            Debug.Assert(holder.OwnerThread == Thread.CurrentThread.ManagedThreadId);

            LifetimeManager.Reset(holder.Object!);

            Interlocked.Exchange(ref holder.OwnerThread, 0);
            FHeldObject.Value = null;
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
