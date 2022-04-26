/********************************************************************************
* Cache.cs                                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
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
            //
            // Dictionary performs mutch better against int keys
            //

            private static ConcurrentDictionary<int, LazySlim> FImplementation { get; } = new();

            //
            // We don't use factory function here since it may get called more than once if the GetOrAdd()
            // invoked with the same key parallelly:
            // https://docs.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentdictionary-2.getoradd
            //

            private sealed class LazySlim
            {
                private readonly Func<TKey, TValue> FFactory;
                private readonly TKey FContext;
                private TValue? FValue;

                public LazySlim(Func<TKey, TValue> factory, TKey context)
                {
                    FFactory = factory;
                    FContext = context;
                }

                public TValue Value
                {
                    get
                    {
                        if (FValue is null)
                            lock (FFactory)
                                FValue ??= FFactory(FContext);
                        return FValue;
                    }
                }
            }

            public static TValue GetOrAdd(TKey key, Func<TKey, TValue> factory, string scope) => FImplementation.GetOrAdd
            (
                #pragma warning disable CA1307 // Specify StringComparison for clarity
                unchecked((key?.GetHashCode() ?? 0) ^ (scope?.GetHashCode() ?? 0)),
                #pragma warning restore CA1307
                new LazySlim(factory, key)
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

        public static TValue GetOrAdd<TKey, TValue>(TKey key, Func<TKey, TValue> factory, [CallerMemberName] string scope = "") => Backend<TKey, TValue>.GetOrAdd(key, factory, scope);
    }
}
