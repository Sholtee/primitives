/********************************************************************************
* EnumerableExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.Primitives
{
    /// <summary>
    /// Defines some extensions for the <see cref="IEnumerable{T}"/> interface.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Iterates through the source collection.
        /// </summary>
        public static void ForEach<T>(this IEnumerable<T> src, Action<T, int> callback)
        {
            if (callback is null)
                throw new ArgumentNullException(nameof(callback));

            int i = 0;
            foreach (T item in src ?? throw new ArgumentNullException(nameof(src)))
            {
                callback(item, i++);
            }
        }
    }
}
