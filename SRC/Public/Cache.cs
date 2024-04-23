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
        private sealed class CacheContext<TKey, TValue>(TKey key, string scope, Func<TKey, TValue> factory)
        {
            public TKey Key { get; } = key;

            public string Scope { get; } = scope;

            public Func<TKey, TValue> Factory { get; } = factory;

            public override int GetHashCode() => unchecked((Key?.GetHashCode() ?? 0) ^ (Scope?.GetHashCode() ?? 0));

            public override bool Equals(object obj) => obj is CacheContext<TKey, TValue> other &&
                EqualityComparer<TKey>.Default.Equals(other.Key, Key) && 
                other.Scope == Scope;
        }

        /// <summary>
        /// Clears the underlying store associated with the given key and value type.
        /// </summary>
        public static void Clear<TKey, TValue>() where TValue: class => CacheSlim.Clear<CacheContext<TKey, TValue>, TValue>();

        /// <summary>
        /// Does what its name suggests.
        /// </summary>
        public static TValue GetOrAdd<TKey, TValue>(TKey key, Func<TKey, TValue> factory, [CallerMemberName] string scope = "") where TValue: class =>
            CacheSlim.GetOrAdd(new CacheContext<TKey, TValue>(key, scope, factory), static ctx => ctx.Factory(ctx.Key));
    }

    /// <summary>
    /// Represents a generic cache where the items have no expiration.
    /// </summary>
    public static class CacheSlim
    {
        private static class Backend<TKey, TValue> where TValue: class
        {
            private static readonly ConcurrentDictionary<TKey, CacheEntry> FImplementation = new();

            //
            // We don't use factory function here since it may get called more than once if the GetOrAdd()
            // invoked with the same key parallelly:
            // https://docs.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentdictionary-2.getoradd
            //

            private sealed class CacheEntry(TKey key, Func<TKey, TValue> factory)
            {
                private TValue? FValue;

                public TValue Value
                {
                    get
                    {
                        if (FValue is null)
                            lock (factory)
                                FValue ??= factory(key);
                        return FValue;
                    }
                }
            }

            public static TValue GetOrAdd(TKey key, Func<TKey, TValue> factory) => FImplementation.GetOrAdd
            (
                key,
                new CacheEntry(key, factory)
            ).Value;

            public static void Clear() => FImplementation.Clear();
        }

        /// <summary>
        /// Clears the underlying store associated with the given key and value type.
        /// </summary>
        public static void Clear<TKey, TValue>() where TValue: class => Backend<TKey, TValue>.Clear();

        /// <summary>
        /// Does what its name suggests.
        /// </summary>
        public static TValue GetOrAdd<TKey, TValue>(TKey key, Func<TKey, TValue> factory) where TValue : class => Backend<TKey, TValue>.GetOrAdd(key, factory);
    }
}
