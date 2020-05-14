/********************************************************************************
* IDisposableEx.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Primitives
{
    /// <summary>
    /// Extends the base <see cref="IDisposable"/> interface with several new features
    /// </summary>
    public interface IDisposableEx: IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Indicates that the current object has already been disposed.
        /// </summary>
        bool Disposed { get; }
    }
}