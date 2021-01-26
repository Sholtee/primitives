/********************************************************************************
* ObjectPool.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Threading;

namespace Solti.Utils.Primitives.Patterns
{
    using Properties;

    /// <summary>
    /// Represents a requested pool item.
    /// </summary>
    #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public class PoolItem<T> : Disposable where T : class
    {
        /// <summary>
        /// The index of the item.
        /// </summary>
        public int Index { get; init; }

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
                Owner.Return(Index);

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
        Throw
    }

    /// <summary>
    /// Describes a simple object pool.
    /// </summary>
    public class ObjectPool<T>: Disposable where T: class
    {
        private readonly SemaphoreSlim FSemaphore;

        private readonly ObjectHolder[] FObjects;

        private struct ObjectHolder
        {
            public int CheckedOut;
            public T? Object;
        }

        /// <summary>
        /// Creates a new <see cref="ObjectPool{T}"/> object.
        /// </summary>
        public ObjectPool(int maxPoolSize, Func<T> factory) 
        {
            FSemaphore = new SemaphoreSlim(0, maxPoolSize);
            FObjects = new ObjectHolder[maxPoolSize];
            Factory = factory;
            MaxSize = maxPoolSize;
        }

        /// <summary>
        /// The maximum number of objects that can be checked out in the same time.
        /// </summary>
        public int MaxSize { get; }

        /// <summary>
        /// Delegate to create pool items.
        /// </summary>
        public Func<T> Factory { get; }

        /// <inheritdoc/>
        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                for (int i = 0; i < MaxSize; i++)
                {
                    ref ObjectHolder holder = ref FObjects[i]; // nem masolat

                    //
                    // Csak annyi elemet probalunk meg felszabaditani amennyi peldanyositva is volt.
                    //

                    if (holder.Object is null)
                        break;

                    try
                    {
                        if (holder.Object is IDisposable disposable)
                            disposable.Dispose();
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine($"Can't dispose pool item: {e}");
                    }
                }

                FSemaphore.Dispose();
            }

            base.Dispose(disposeManaged);
        }

        /// <summary>
        /// Gets an item from the pool.
        /// </summary>
        public PoolItem<T> Get(CheckoutPolicy checkoutPolicy = CheckoutPolicy.Block, CancellationToken cancellation = default) => new PoolItem<T>
        {
            Value = Get(out int index, checkoutPolicy, cancellation),
            Index = index,
            Owner = this
        };

        /// <summary>
        /// Gets an item from the pool.
        /// </summary>
        public T Get(out int index, CheckoutPolicy checkoutPolicy = CheckoutPolicy.Block, CancellationToken cancellation = default)
        {
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
                // Igen es mivel a kerest egybol ki kellett vna szolgalni ezert kivetel.
                //

                Debug.Assert(checkoutPolicy == CheckoutPolicy.Throw);

                throw new InvalidOperationException(Resources.POOL_SIZE_REACHED);
            }

            //
            // Ha ki tudjuk szolgalni a kerest egy korabbi elemmel akkor visszaadjuk azt,
            // kulonben letrehozunk egy ujat.
            //

            for (index = 0; index < MaxSize; index++)
            {
                ref ObjectHolder holder = ref FObjects[index]; // nem masolat

                //
                // Az elso olyan elem ami meg nincs kicsekkolva
                //

                if (Interlocked.CompareExchange(ref holder.CheckedOut, -1, 0) == 0)
                    //
                    // Lehet h meg nem is vt legyartva hozza elem, akkor legyartjuk
                    //

                    return holder.Object ??= Factory();
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
        public void Return(int index) 
        {
            ref ObjectHolder holder = ref FObjects[index]; // nem masolat

            if (holder.Object is IResettable resettable)
                resettable.Reset();

            Interlocked.Exchange(ref holder.CheckedOut, 0);
        }
    }
}
