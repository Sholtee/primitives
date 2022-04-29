/********************************************************************************
* DisposableSupportsNotifyOnDispose.cs                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading.Tasks;

namespace Solti.Utils.Primitives.Patterns
{
    /// <summary>
    /// Describes a disposable object that implements the <see cref="INotifyOnDispose"/> interface
    /// </summary>
    public class DisposableSupportsNotifyOnDispose : Disposable, INotifyOnDispose
    {
        /// <summary>
        /// Creates a new <see cref="DisposableSupportsNotifyOnDispose"/> instance.
        /// </summary>
        public DisposableSupportsNotifyOnDispose(bool supportFinalizer = false) : base(supportFinalizer) { }

        /// <inheritdoc/>
        public event EventHandler<bool>? OnDispose;

        /// <inheritdoc/>
        protected override void Dispose(bool disposeManaged)
        {
            OnDispose?.Invoke(this, disposeManaged);
            base.Dispose(disposeManaged);
        }

        /// <inheritdoc/>
        protected override ValueTask AsyncDispose()
        {
            OnDispose?.Invoke(this, true);
            return default;
        }
    }
}
