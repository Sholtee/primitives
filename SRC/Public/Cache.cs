﻿/********************************************************************************
* Cache.cs                                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

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
            public static ConcurrentDictionary<(TKey, string), Lazy<TValue>> Value { get; } = new();     
        }

        /// <summary>
        /// Gets the underlying dictionary associated with the given key and value type.
        /// </summary>
        /// <remarks>This method returns a snapshot.</remarks>
        public static IDictionary<(TKey, string), TValue> AsDictionary<TKey, TValue>() => Backend<TKey, TValue>
            .Value
            .ToDictionary(entry => entry.Key, entry => entry.Value.Value);

        /// <summary>
        /// Clears the underlying store associated with the given key and value type.
        /// </summary>
        public static void Clear<TKey, TValue>() => Backend<TKey, TValue>.Value.Clear();

        /// <summary>
        /// Does what its name suggests.
        /// </summary>
        public static TValue GetOrAdd<TKey, TValue>(TKey key, Func<TValue> factory, [CallerMemberName] string scope = "") =>  Backend<TKey, TValue>
            .Value
            //
            // Ne direktben adjuk at a "factory"-t mert az tobbszor is meghivasra kerulhet [ha ugyanazzal a kulccsal parhuzamosan
            // huvjuk a GetOrAdd()-t]:
            // https://docs.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentdictionary-2.getoradd
            //

            .GetOrAdd((key, scope), _ => new Lazy<TValue>(factory, LazyThreadSafetyMode.ExecutionAndPublication)).Value;
    }
}