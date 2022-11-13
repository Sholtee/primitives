/********************************************************************************
* RedBlackTreeExtensions.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Solti.Utils.Primitives
{
    using Patterns;

    /// <summary>
    /// Defines some extensions against the <see cref="RedBlackTree{TData}"/> class
    /// </summary>
    public static class RedBlackTreeExtensions
    {
        private sealed class KVPComparer<TKey, TValue> : Singleton<KVPComparer<TKey, TValue>>, IComparer<KeyValuePair<TKey, TValue>> where TKey: IComparable<TKey>
        {
            public int Compare(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y) => x.Key.CompareTo(y.Key);
        }

        private static bool TryGet<TKey, TValue>(RedBlackTreeNode<KeyValuePair<TKey, TValue>>? node, TKey key, out TValue result) where TKey : IComparable<TKey>
        {
            if (node is null)
            {
                result = default!;
                return false;
            }

            int order = key.CompareTo(node.Data.Key);

            if (order < 0)
                return TryGet(node.Left, key, out result);

            if (order > 0)
                return TryGet(node.Right, key, out result);

            result = node.Data.Value;
            return true;
        }

        /// <summary>
        /// Creates a red-black tree intended for value lookup.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RedBlackTree<KeyValuePair<TKey, TValue>> CreateLookup<TKey, TValue>() where TKey : IComparable<TKey> => new
        (
            KVPComparer<TKey, TValue>.Instance
        );

        /// <summary>
        /// Tries to get a value associated with the given key.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGet<TKey, TValue>(this RedBlackTree<KeyValuePair<TKey, TValue>> src, TKey key, out TValue result) where TKey : IComparable<TKey> => TryGet
        (
            src.Root,
            key,
            out result
        );

        /// <summary>
        /// Tries to add a value to the lookup.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAdd<TKey, TValue>(this RedBlackTree<KeyValuePair<TKey, TValue>> src, TKey key, TValue value) where TKey : IComparable<TKey> => src.Add
        (
            new KeyValuePair<TKey, TValue>(key, value)
        );

        /// <summary>
        /// Clones the given tree.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RedBlackTree<TData> Clone<TData>(this RedBlackTree<TData> src)
        {
            RedBlackTree<TData> clone = new(src.Comparer);

            foreach (RedBlackTreeNode<TData> node in src)
            {
                clone.Add(node.Data);
            }

            return clone;
        }
    }
}
