/********************************************************************************
* DisposeByRefObject.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading.Tasks;

namespace Solti.Utils.Primitives.Patterns
{
    using static Threading.InterlockedExtensions;

    using Properties;

    /// <summary>
    /// Manages object lifetime by refence counting.
    /// </summary>
    public class DisposeByRefObject : Disposable
    {
        private int FRefCount = 1;

        /// <inheritdoc/>
        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged && FRefCount > 0)
                throw new InvalidOperationException(Resources.ARBITRARY_RELEASE);

            base.Dispose(disposeManaged);
        }

        /// <inheritdoc/>
        protected override ValueTask AsyncDispose()
        {
            if (FRefCount > 0)
                throw new InvalidOperationException(Resources.ARBITRARY_RELEASE);

            return default;
        }

        /// <summary>
        /// The current reference count.
        /// </summary>
        public int RefCount => FRefCount;

        /// <summary>
        /// Increments the reference counter as an atomic operation.
        /// </summary>
        /// <returns>The current reference count.</returns>
        public int AddRef()
        {
            int? refCount = IncrementIfGreaterThan(ref FRefCount, 0) + 1; // NULL + 1 == NULL

            if (refCount is null)
                throw new ObjectDisposedException(null);

            return refCount.Value;
        }

        /// <summary>
        /// Decrements the reference counter as an atomic operation and disposes the object if the reference count reached the zero.
        /// </summary>
        /// <returns>The current reference count.</returns>
        public int Release()
        {
            int? refCount = DecrementIfGreaterThan(ref FRefCount, 0) - 1; // NULL - 1 == NULL

            if (refCount is null)
                throw new ObjectDisposedException(null);

            if (refCount is 0)
                Dispose();

            return refCount.Value;
        }

        /// <summary>
        /// Decrements the reference counter as an atomic operation and disposes the object asynchronously if the reference count reached the zero.
        /// </summary>
        /// <returns>The current reference count.</returns>
        public async Task<int> ReleaseAsync() 
        {
            int? refCount = DecrementIfGreaterThan(ref FRefCount, 0) - 1;

            if (refCount is null)
                throw new ObjectDisposedException(null);

            if (refCount is 0)
                await DisposeAsync();

            return refCount.Value;
        }
    }
}
