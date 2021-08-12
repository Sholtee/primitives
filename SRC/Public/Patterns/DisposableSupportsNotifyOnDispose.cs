/********************************************************************************
* DisposableSupportsNotifyOnDispose.cs                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Primitives.Patterns
{
    /// <summary>
    /// Describes a disposable object that implements the <see cref="INotifyOnDispose"/> interface
    /// </summary>
    public class DisposableSupportsNotifyOnDispose : Disposable, INotifyOnDispose
    {
        /// <inheritdoc/>
        public event EventHandler? OnDispose;

        /// <inheritdoc/>
        protected override void BeforeDispose()
        {
            base.BeforeDispose();
            OnDispose?.Invoke(this, EventArgs.Empty);
        }
    }
}
