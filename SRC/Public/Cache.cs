/********************************************************************************
* Cache.cs                                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Solti.Utils.Primitives
{
    /// <summary>
    /// Represents a generic cache where the items have no expiration.
    /// </summary>
    [SuppressMessage("Naming", "CA1724:Type names should not match namespaces")]
    public static class Cache 
    {
        private static class Backend<TKey, TValue> 
        {
            public static ConcurrentDictionary<(TKey, string), TValue> Value { get; } = new ConcurrentDictionary<(TKey, string), TValue>();     
        }

        /// <summary>
        /// Gets the underlying dictionary associated with the given key and value type.
        /// </summary>
        public static IDictionary<(TKey, string), TValue> AsDictionary<TKey, TValue>() => Backend<TKey, TValue>.Value;

        /// <summary>
        /// Does what its name suggests.
        /// </summary>
        public static TValue GetOrAdd<TKey, TValue>(TKey key, Func<TValue> factory, [CallerMemberName] string scope = "") => 
            Backend<TKey, TValue>.Value.GetOrAdd((key, scope), @void => factory());
    }
}
