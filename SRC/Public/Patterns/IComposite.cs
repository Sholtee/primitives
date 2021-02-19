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
    /// <typeparam name="TInterface">The interface on which we want to apply the pattern.</typeparam>
    public interface IComposite<TInterface>: IDisposableEx where TInterface : class, IComposite<TInterface>
    {
        /// <summary>
        /// The parent of this entity.
        /// </summary>
        TInterface? Parent { get; set; }

        /// <summary>
        /// The children of this entity.
        /// </summary>
        /// <remarks>All members of this property should be thread safe.</remarks>
        ICollection<TInterface> Children { get; }
    }
}