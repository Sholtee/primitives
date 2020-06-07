/********************************************************************************
* SafeEnumerator.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Solti.Utils.Primitives.Threading
{
    using Patterns;

    /// <summary>
    /// Thread safe <see cref="IEnumerator{T}"/> implementation.
    /// </summary>
    public sealed class SafeEnumerator<T> : Disposable, IEnumerator<T>
    {
        private readonly IEnumerator<T> FUnderlyingEnumerator;
        private readonly ReaderWriterLockSlim FLock;

        /// <summary>
        /// Creates a new <see cref="SafeEnumerator{T}"/> instane.
        /// </summary>
        public SafeEnumerator(IEnumerable<T> src, ReaderWriterLockSlim @lock) 
        {
            Ensure.Parameter.IsNotNull(src, nameof(src));
            Ensure.Parameter.IsNotNull(@lock, nameof(@lock));

            @lock.EnterReadLock();
            FLock = @lock;

            FUnderlyingEnumerator = src.GetEnumerator();                            
        }

        /// <summary>
        /// See <see cref="IEnumerator{T}.Current"/>.
        /// </summary>
        public T Current => FUnderlyingEnumerator.Current;

        /// <summary>
        /// See <see cref="IEnumerator.Current"/>.
        /// </summary>
        object? IEnumerator.Current => FUnderlyingEnumerator.Current;

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        protected override void Dispose(bool disposeManaged)
        {
            //
            // Ne ellenorizzuk a "disposeManaged" erteket h a lock-ot akkor is eleresszuk 
            // ha a GetEnumerator() a konstruktorban kivetelt dobott.
            //

            FUnderlyingEnumerator?.Dispose();
            FLock.ExitReadLock();

            base.Dispose(disposeManaged);
        }

        /// <summary>
        /// See <see cref="IEnumerator.MoveNext"/>.
        /// </summary>
        public bool MoveNext() => FUnderlyingEnumerator.MoveNext();

        /// <summary>
        /// See <see cref="IEnumerator.Reset"/>.
        /// </summary>
        public void Reset() => FUnderlyingEnumerator.Reset();
    }
}
