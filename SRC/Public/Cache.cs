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
            private static readonly ConcurrentDictionary<CacheEntry, CacheEntry> FImplementation = new();

            //
            // We don't use factory function here since it may get called more than once if the GetOrAdd()
            // invoked with the same key parallelly:
            // https://docs.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentdictionary-2.getoradd
            //

            private sealed class CacheEntry
            {
                private readonly Func<TKey, TValue> FFactory;
                private readonly TKey FKey;
                private readonly string FScope;
                private readonly int FHashCode;
                private TValue? FValue;

                public CacheEntry(TKey key, string scope, Func<TKey, TValue> factory)
                {
                    FFactory = factory;
                    FKey = key;
                    FScope = scope;
                    #pragma warning disable CA1307 // Specify StringComparison for clarity
                    FHashCode = unchecked((key?.GetHashCode() ?? 0) ^ (scope?.GetHashCode() ?? 0));
                    #pragma warning restore CA1307
                }

                public TValue Value
                {
                    get
                    {
                        if (FValue is null)
                            lock (FFactory)
                                FValue ??= FFactory(FKey);
                        return FValue;
                    }
                }

                public override int GetHashCode() => FHashCode;

                public override bool Equals(object obj)
                {
                    CacheEntry that = (CacheEntry) obj;

                    return
                        that.FHashCode == FHashCode &&

                        //
                        // Comparing the hashcodes is not enough since special types (e.g.: delegates) cannot be distinguished by hash.
                        //

                        EqualityComparer<TKey>.Default.Equals(that.FKey, FKey) && 
                        that.FScope == FScope;
                }
            }

            public static TValue GetOrAdd(TKey key, string scope, Func<TKey, TValue> factory)
            {
                CacheEntry entry = new(key, scope, factory);

                return FImplementation.GetOrAdd(entry,  entry).Value;
            }

            public static void Clear() => FImplementation.Clear();
        }

        /// <summary>
        /// Clears the underlying store associated with the given key and value type.
        /// </summary>
        public static void Clear<TKey, TValue>() => Backend<TKey, TValue>.Clear();

        /// <summary>
        /// Does what its name suggests.
        /// </summary>
        public static TValue GetOrAdd<TKey, TValue>(TKey key, Func<TKey, TValue> factory, [CallerMemberName] string scope = "") => Backend<TKey, TValue>.GetOrAdd(key, scope, factory);
    }

    /// <summary>
    /// Represents a generic cache where the items have no expiration.
    /// </summary>
    public static class CacheSlim
    {
        private static class Backend<TKey, TValue>
        {
            //
            // Dictionary performs much better against int keys.
            //

            private static readonly ConcurrentDictionary<int, CacheEntry> FImplementation = new();

            //
            // We don't use factory function here since it may get called more than once if the GetOrAdd()
            // invoked with the same key parallelly:
            // https://docs.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentdictionary-2.getoradd
            //

            private sealed class CacheEntry
            {
                private readonly Func<TKey, TValue> FFactory;
                private readonly TKey FKey;
                private TValue? FValue;

                public CacheEntry(TKey key, Func<TKey, TValue> factory)
                {
                    FFactory = factory;
                    FKey = key;
                }

                public TValue Value
                {
                    get
                    {
                        if (FValue is null)
                            lock (FFactory)
                                FValue ??= FFactory(FKey);
                        return FValue;
                    }
                }
            }

            public static TValue GetOrAdd(TKey key, string scope, Func<TKey, TValue> factory) => FImplementation.GetOrAdd
            (
                #pragma warning disable CA1307 // Specify StringComparison for clarity
                unchecked((key?.GetHashCode() ?? 0) ^ (scope?.GetHashCode() ?? 0)),
                #pragma warning restore CA1307
                new CacheEntry(key, factory)
            ).Value;

            public static void Clear() => FImplementation.Clear();
        }

        /// <summary>
        /// Clears the underlying store associated with the given key and value type.
        /// </summary>
        public static void Clear<TKey, TValue>() => Backend<TKey, TValue>.Clear();

        /// <summary>
        /// Does what its name suggests.
        /// </summary>
        public static TValue GetOrAdd<TKey, TValue>(TKey key, Func<TKey, TValue> factory, [CallerMemberName] string scope = "") => Backend<TKey, TValue>.GetOrAdd(key, scope, factory);
    }
}
