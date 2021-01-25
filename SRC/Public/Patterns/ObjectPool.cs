/********************************************************************************
* ObjectPool.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace Solti.Utils.Primitives.Patterns
{
    using Properties;

    /// <summary>
    /// Represents a requested pool item.
    /// </summary>
    public class PoolItem<T> where T : class 
    {
        internal  PoolItem(T item, ObjectPool<T> owner) 
        {
            Item = item;
            Owner = owner;
        }

        internal ObjectPool<T> Owner { get; } 

        /// <summary>
        /// The requested item.
        /// </summary>
        public T Item { get; private set; }

        /// <summary>
        /// Returns the object to the pool if there is enough space in it, discards otherwise.
        /// </summary>
        public void Return() 
        {
            //
            // Mar vissza lett teve az elem?
            //

            if (Item is null)
                throw new InvalidOperationException();

            Owner.Return(Item);
            Item = null!;
        }
    }

    /// <summary>
    /// Describes the <see cref="ObjectPool{T}.Get(CancellationToken)"/> behavior when the request can not be granted.
    /// </summary>
    public enum CheckoutPolicy 
    {
        /// <summary>
        /// <see cref="ObjectPool{T}.Get(CancellationToken)"/> blocks until it can serve the request.
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
        /// <summary>
        /// Creates a new <see cref="ObjectPool{T}"/> object.
        /// </summary>
        public ObjectPool(int maxPoolSize, Func<T> factory) 
        {
            Semaphore = new SemaphoreSlim(0, MaxSize = maxPoolSize);
            Objects = new ConcurrentBag<T>();
            Factory = factory;
        }

        /// <summary>
        /// The maximum number of objects that can be checked out in the same time.
        /// </summary>
        public int MaxSize { get; }

        /// <summary>
        /// Delegate to create pool items.
        /// </summary>
        public Func<T> Factory { get; }

        /// <summary>
        /// Gets an item from the pool.
        /// </summary>
        public PoolItem<T> Get(CancellationToken cancellation = default) => new PoolItem<T>
        (
            Get(true, cancellation), 
            this
        );

        /// <inheritdoc/>
        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
                Semaphore.Dispose();
            base.Dispose(disposeManaged);
        }

        internal ConcurrentBag<T> Objects { get; }

        internal SemaphoreSlim Semaphore { get; }

        internal T Get(bool wait, CancellationToken cancellation)
        {
            //
            // Elertuk a maximalis meretet?
            //

            if (!Semaphore.Wait(wait ? Timeout.Infinite : 0, cancellation))
            {
                //
                // Igen, de a kerest nem kellett vna azonnal kiszolgalni ezert a szal blokkolasra kerult -> varakozas meg lett szakitva.
                //

                cancellation.ThrowIfCancellationRequested();

                //
                // Igen es mivel a kerest egybol ki kellett vna szolgalni ezert kivetel.
                //

                Debug.Assert(!wait);

                throw new InvalidOperationException(Resources.POOL_SIZE_REACHED);
            }

            //
            // Ha ki tudjuk szolgalni a kerest egy korabbi elemmel akkor visszaadjuk azt,
            // kulonben letrehozunk egy ujat.
            //

            return Objects.TryTake(out T item)
                ? item
                : Factory();
        }

        internal void Return(T item) 
        {
            //
            // Allapot visszaallitas
            //

            if (item is IResettable resettable)
                resettable.Reset();

            //
            // Oljektum visszaadasa a szulonek
            //

            Objects.Add(item);
            Semaphore.Release();
        }
    }
}
