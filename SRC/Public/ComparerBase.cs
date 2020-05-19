/********************************************************************************
* ComparerBase.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;

namespace Solti.Utils.Primitives
{
    /// <summary>
    /// Defines the base class of comparers.
    /// </summary>
    public abstract class ComparerBase<TConcreteComparer, T> : IEqualityComparer<T> where TConcreteComparer : ComparerBase<TConcreteComparer, T>, new()
    {
        /// <summary>
        /// Implements the <see cref="IEqualityComparer{T}.Equals(T, T)"/> method.
        /// </summary>
        public virtual bool Equals(T x, T y) => ReferenceEquals(x, y) || GetHashCode(x) == GetHashCode(y);

        /// <summary>
        /// The abstract implementation for the <see cref="IEqualityComparer{T}.GetHashCode(T)"/> method.
        /// </summary>
        public abstract int GetHashCode(T obj);

        /// <summary>
        /// The thread safe instance of this comparer
        /// </summary>
        [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Every descendant must have its own Instance value.")]
        public static TConcreteComparer Instance { get; } = new TConcreteComparer();
    }
}
