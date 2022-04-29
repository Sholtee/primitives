/********************************************************************************
* Disposable.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Solti.Utils.Primitives.Patterns
{
    using Threading;

    /// <summary>
    /// Implements the <see cref="IDisposable"/> and <see cref="IAsyncDisposable"/> interfaces.
    /// </summary>
    public class Disposable : IDisposableEx
    {
        //
        // Constructing objects having finalizer may take long (see benchmarks).
        //

        private sealed class FinalizerImplementation
        {
            public readonly Disposable Target;

            public FinalizerImplementation(Disposable target) => Target = target;

            ~FinalizerImplementation()
            {
                Trace.WriteLine($"{Target.GetType().GetFriendlyName()} is disposed by GC. You may be missing a Dispose() call.");
                Target.Dispose(false);
            }
        }

        [Flags]
        private enum DisposableStates: int
        {
            Default = 0,
            Disposing = 1,
            Disposed = 2
        }

        private int FState;

        private readonly FinalizerImplementation? FFinalizer;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool SetDisposing()
        {
            //
            // According to MSDN, we can't throw ObjectDisposedException in Dispose() being called
            // more then once.
            // Interlocked to support parallel case.
            //

            int prevState = InterlockedExtensions.Or(ref FState, (int) DisposableStates.Disposing);

            return (prevState & (int) DisposableStates.Disposing) is 0;
        }

        /// <summary>
        /// Indicates whether the object was disposed or not.
        /// </summary>
        public bool Disposed => (FState & (int) DisposableStates.Disposed) is not 0;

        /// <summary>
        /// Indicates whether the object disposal logic has already been invoked or not.
        /// </summary>
        public bool Disposing => (FState & (int) DisposableStates.Disposing) is not 0;

        /// <summary>
        /// Method to be overridden to implement custom disposal logic.
        /// </summary>
        /// <param name="disposeManaged">It is set to true on <see cref="IDisposable.Dispose"/> call.</param>
        protected virtual void Dispose(bool disposeManaged) { }


        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting resources asynchronously
        /// </summary>
        [SuppressMessage("Performance", "CA1849:Call async methods when in an async method")]
        protected virtual ValueTask AsyncDispose() 
        {
            Dispose(true);
            return default;
        }

        /// <summary>
        /// Throws if the current instance has already been disposed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void CheckNotDisposed() 
        {
            if (Disposed)
                throw new ObjectDisposedException(null);
        }

        /// <summary>
        /// Creates a new <see cref="Disposable"/> instance.
        /// </summary>
        public Disposable(bool supportFinalizer = false)
        {
            if (supportFinalizer)
                FFinalizer = new FinalizerImplementation(this);
        }

        /// <summary>
        /// Implements the <see cref="IDisposable.Dispose"/> method.
        /// </summary>
        [SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "The method is implemented correctly.")]
        [SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "SuppressFinalize is called on the outsourced implementation")]
        public void Dispose()
        {
            if (SetDisposing())
            {
                Dispose(disposeManaged: true);

                if (FFinalizer is not null)
                    GC.SuppressFinalize(FFinalizer);

                FState |= (int) DisposableStates.Disposed;
            }
        }

        /// <summary>
        /// Implements the <see cref="IAsyncDisposable.DisposeAsync"/> method.
        /// </summary>
        [SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "SuppressFinalize is called on the outsourced implementation")]
        public async ValueTask DisposeAsync()
        {
            if (SetDisposing())
            {
                await AsyncDispose();

                if (FFinalizer is not null)
                    GC.SuppressFinalize(FFinalizer);

                FState |= (int) DisposableStates.Disposed;
            }
        }
    }
}
