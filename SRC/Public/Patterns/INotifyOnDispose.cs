﻿/********************************************************************************
* INotifyOnDispose.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Primitives.Patterns
{
    /// <summary>
    /// Notifies the clients if the object is about to dispose.
    /// </summary>
    public interface INotifyOnDispose
    {
        /// <summary>
        /// Fires if the object is about to dispose.
        /// </summary>
        #pragma warning disable CA1003 // Use generic event handler instances
        event EventHandler<bool>? OnDispose;
        #pragma warning restore CA1003
    }
}