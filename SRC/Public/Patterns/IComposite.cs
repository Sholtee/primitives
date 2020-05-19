/********************************************************************************
* IComposite.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.Primitives.Patterns
{
    /// <summary>
    /// Describes the composite pattern.
    /// </summary>
    /// <typeparam name="T">The type on which we want to apply the pattern.</typeparam>
    /// <remarks>This is an internal interface so it may change from version to version. Don't use it!</remarks>
    public interface IComposite<T>: IDisposableEx where T : class, IComposite<T>
    {
        /// <summary>
        /// The parent of this entity.
        /// </summary>
        T? Parent { get; set; }

        /// <summary>
        /// The children of this entity.
        /// </summary>
        /// <remarks>All members of this property should be thread safe.</remarks>
        ICollection<T> Children { get; }
    }
}