/********************************************************************************
* Disposable.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Solti.Utils.Primitives.Patterns
{
    using static Threading.InterlockedExtensions;

    /// <summary>
    /// Implements the <see cref="IDisposable"/> and <see cref="IAsyncDisposable"/> interfaces.
    /// </summary>
    public class Disposable : IDisposableEx
    {
        [Flags]
        private enum DisposableStates
        {
            Default = 0,
            Disposing = 1,
            Disposed = 2
        }

        private int FState;

        /// <summary>
        /// Indicates whether the object was disposed or not.
        /// </summary>
        public bool Disposed => (FState & (int) DisposableStates.Disposed) is not 0;

        /// <summary>
        /// Method to be overridden to implement custom disposal logic.
        /// </summary>
        /// <param name="disposeManaged">It is set to true on <see cref="IDisposable.Dispose"/> call.</param>
        protected virtual void Dispose(bool disposeManaged) { }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting resources asynchronously
        /// </summary>
        protected virtual ValueTask AsyncDispose() 
        {
            Dispose(true);
            return default;
        }

        /// <summary>
        /// Invoked before actual disposal logic.
        /// </summary>
        protected virtual void BeforeDispose()
        {
        }

        /// <summary>
        /// Throws if the current instance has already been disposed.
        /// </summary>
        protected void CheckNotDisposed() 
        {
            if (Disposed) throw new ObjectDisposedException(null);
        }

        /// <summary>
        /// Destructor of this class.
        /// </summary>
        [SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "The method is implemented correctly.")]
        ~Disposable()
        {
            Trace.WriteLine($"{GetType().GetFriendlyName()} is disposed by GC. You may be missing a Dispose() call.");
            Dispose(disposeManaged: false);
        }

        /// <summary>
        /// Implements the <see cref="IDisposable.Dispose"/> method.
        /// </summary>
        [SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "The method is implemented correctly.")]
        public void Dispose()
        {
            //
            // MSDN szerint nem dobhatunk ObjectDisposedException-t ha a metodus egynel tobbszor volt meghivva
            // (Interlocked hogy a parhuzamos eseteket is jol kezeljuk)
            //

            if ((Or(ref FState, (int) DisposableStates.Disposing) & (int) DisposableStates.Disposing) is not 0)
                return;

            BeforeDispose();

            Dispose(disposeManaged: true);

            GC.SuppressFinalize(this);
            FState |= (int) DisposableStates.Disposed;
        }

        /// <summary>
        /// Implements the <see cref="IAsyncDisposable.DisposeAsync"/> method.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            //
            // MSDN szerint nem dobhatunk ObjectDisposedException-t ha a metodus egynel tobbszor volt meghivva
            // (Interlocked hogy a parhuzamos eseteket is jol kezeljuk)
            //

            if ((Or(ref FState, (int) DisposableStates.Disposing) & (int) DisposableStates.Disposing) is not 0)
                return;

            BeforeDispose();

            await AsyncDispose();

            GC.SuppressFinalize(this);
            FState |= (int) DisposableStates.Disposed;
        }
    }
}
