/********************************************************************************
* Cache.cs                                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Solti.Utils.Primitives
{
    /// <summary>
    /// Represents a generic cache where the items have no expiration.
    /// </summary>
    public static class Cache 
    {
        private static class Backend<TKey, TValue> 
        {
            public static ConcurrentDictionary<(TKey Key, string Scope), TValue> Value { get; } = new ConcurrentDictionary<(TKey Key, string Scope), TValue>();     
        }

        /// <summary>
        /// Does what its name suggests.
        /// </summary>
        public static TValue GetOrAdd<TKey, TValue>(TKey key, Func<TValue> factory, [CallerMemberName] string scope = "") => 
            Backend<TKey, TValue>.Value.GetOrAdd((key, scope), @void => factory());
    }
}
