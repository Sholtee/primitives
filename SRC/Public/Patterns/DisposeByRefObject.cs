/********************************************************************************
* DisposeByRefObject.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Solti.Utils.Primitives.Patterns
{
    using Properties;

    /// <summary>
    /// Manages object lifetime by refence counting.
    /// </summary>
    public class DisposeByRefObject : Disposable
    {
        private readonly object FLock = new object();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureNotDisposed() 
        {
            if (RefCount == 0)
                throw new ObjectDisposedException(null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureDisposeAllowed() 
        {
            if (RefCount != 0)
                throw new InvalidOperationException(Resources.ARBITRARY_RELEASE);
        }

        /// <summary>
        /// See <see cref="Disposable"/> class.
        /// </summary>
        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged) EnsureDisposeAllowed();
            base.Dispose(disposeManaged);
        }

        /// <summary>
        /// See <see cref="Disposable"/> class.
        /// </summary>
        protected override ValueTask AsyncDispose()
        {
            EnsureDisposeAllowed();
            return base.AsyncDispose();
        }

        /// <summary>
        /// The current reference count.
        /// </summary>
        public int RefCount { get; private set; } = 1;

        /// <summary>
        /// Increments the reference counter as an atomic operation.
        /// </summary>
        /// <returns>The current reference count.</returns>
        public int AddRef()
        {
            lock (FLock)
            {
                EnsureNotDisposed();
                return ++RefCount;
            }
        }

        /// <summary>
        /// Decrements the reference counter as an atomic operation and disposes the object if the reference count reached the zero.
        /// </summary>
        /// <returns>The current reference count.</returns>
        public int Release()
        {
            lock (FLock) 
            {
                EnsureNotDisposed();
                if (--RefCount > 0) return RefCount;
            }

            Dispose();
            return 0;
        }

        /// <summary>
        /// Decrements the reference counter as an atomic operation and disposes the object asynchronously if the reference count reached the zero.
        /// </summary>
        /// <returns>The current reference count.</returns>
        public async Task<int> ReleaseAsync() 
        {
            lock (FLock) 
            {
                EnsureNotDisposed();
                if (--RefCount > 0) return RefCount;
            }

            await DisposeAsync();
            return 0;
        }
    }
}
