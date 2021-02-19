/********************************************************************************
* ObjectPool.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Solti.Utils.Primitives.Threading
{
    using Patterns;
    using Properties;

    /// <summary>
    /// Represents a requested pool item.
    /// </summary>
    #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public class PoolItem<T> : Disposable where T : class
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
    /// Describes a simple object pool.
    /// </summary>
    public class ObjectPool<T>: Disposable, IReadOnlyCollection<(int OwnerThread, T Object)> where T: class
    {
        private readonly SemaphoreSlim FSemaphore;

        private readonly (int OwnerThread, T? Object)[] FObjects;

        private readonly ThreadLocal<GetObjectHolder?> FHeldObject;

        private delegate ref (int OwnerThread, T? Object) GetObjectHolder();

        /// <summary>
        /// Creates a new <see cref="ObjectPool{T}"/> object.
        /// </summary>
        public ObjectPool(int maxPoolSize, Func<T> factory, bool suppressItemDispose = false) 
        {
            FSemaphore = new SemaphoreSlim(maxPoolSize, maxPoolSize);
            FObjects = new (int OwnerThread, T? Object)[maxPoolSize]; // mivel Tuple-k ertek tipusok ezert nincs gond az inicialassal
            FHeldObject = new ThreadLocal<GetObjectHolder?>(trackAllValues: false);
            Factory = factory;
            Capacity = maxPoolSize;
            SuppressItemDispose = suppressItemDispose;
        }

        /// <summary>
        /// The maximum number of objects that can be checked out in the same time.
        /// </summary>
        public int Capacity { get; }

        /// <summary>
        /// Delegate to create pool items.
        /// </summary>
        public Func<T> Factory { get; }

        /// <summary>
        /// Returns true if the <see cref="ObjectPool{T}"/> should NOT dispose its items on release.
        /// </summary>
        public bool SuppressItemDispose { get; }

        /// <inheritdoc/>
        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                if (!SuppressItemDispose) foreach ((int _, T Object) item in this) // csak a felhasznalt elemeket adja vissza
                {
                    try
                    {
                        if (item.Object is IDisposable disposable)
                            disposable.Dispose();
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine($"Can't dispose pool item: {e}");
                    }
                }

                FSemaphore.Dispose();
                FHeldObject.Dispose();
            }

            base.Dispose(disposeManaged);
        }

        /// <summary>
        /// Gets an item from the pool.
        /// </summary>
        public PoolItem<T>? GetItem(CheckoutPolicy checkoutPolicy = CheckoutPolicy.Block, CancellationToken cancellation = default)
        {
            T? value = Get(checkoutPolicy, cancellation);

            return value is null
                ? null
                : new PoolItem<T>
                {
                    Value = value,
                    Owner = this
                };
        }

        /// <summary>
        /// Gets a value from the pool.
        /// </summary>
        public T? Get(CheckoutPolicy checkoutPolicy = CheckoutPolicy.Block, CancellationToken cancellation = default)
        {
            //
            // Szalankent csak egyszer vehetunk ki a pool-bol elemet
            //

            if (FHeldObject.Value is not null)
                return FHeldObject.Value().Object;

            //
            // Elertuk a maximalis meretet?
            //

            if (!FSemaphore.Wait(checkoutPolicy == CheckoutPolicy.Block ? Timeout.Infinite : 0, cancellation))
            {
                //
                // Igen, de a kerest nem kellett vna azonnal kiszolgalni ezert a szal blokkolasra kerult -> varakozas meg lett szakitva.
                //

                cancellation.ThrowIfCancellationRequested();

                //
                // Igen es mivel a kerest egybol ki kellett vna szolgalni ezert vagy kivetelt v NULL-t adunk vissza.
                //

                if (checkoutPolicy == CheckoutPolicy.Throw)
                    throw new InvalidOperationException(Resources.POOL_SIZE_REACHED);

                Debug.Assert(checkoutPolicy == CheckoutPolicy.Discard);
                return default;
            }

            //
            // Ha ki tudjuk szolgalni a kerest egy korabbi elemmel akkor visszaadjuk azt,
            // kulonben letrehozunk egy ujat.
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

                    //
                    // Lehet h meg nem is vt legyartva hozza elem, akkor legyartjuk
                    //

                    return holder.Object ??= Factory();
                }
            }

            //
            // Ide sose lenne szabad eljussunk a semaphore miatt
            //

            Debug.Fail("No empty slot in the pool");
            return default!;
        }

        /// <summary>
        /// Returns the item to the pool, identified by its index.
        /// </summary>
        public void Return() 
        {
            if (FHeldObject.Value is null)
                return;

            ref (int OwnerThread, T? Object) holder = ref FHeldObject.Value(); // nem masolat

            if (holder.Object is IResettable resettable)
                resettable.Reset();

            Interlocked.Exchange(ref holder.OwnerThread, 0);

            FSemaphore.Release();
            FHeldObject.Value = null;
        }

        /// <summary>
        /// See <see cref="IEnumerable{T}.GetEnumerator"/>.
        /// </summary>
        public IEnumerator<(int OwnerThread, T Object)> GetEnumerator()
        {
            for (int i = 0; i < Capacity; i++)
            {
                //
                // Masolat, ami nekunk jo is mert igy a visszaadott elem szabadon modosithato
                //

                (int OwnerThread, T? Object) holder = FObjects[i];

                //
                // Csak a hasznalatban levo elemeket adjuk vissza.
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
        public int Count => Capacity - FSemaphore.CurrentCount;
    }
}
