/********************************************************************************
* IDisposableEx.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.Primitives.Patterns
{
    /// <summary>
    /// Extends the base <see cref="IDisposable"/> interface with several new features
    /// </summary>
    [SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "To preserve the backward compatibility we should keep the 'Ex' suffix.")]
    public interface IDisposableEx: IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Indicates that the current object has already been disposed.
        /// </summary>
        bool Disposed { get; }
    }
}