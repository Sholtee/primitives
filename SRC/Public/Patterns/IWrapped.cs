/********************************************************************************
* IWrapped.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Primitives.Patterns
{
    /// <summary>
    /// Represents a wrapped object.
    /// </summary>
    /// <remarks>Disposing this object will release the underlying object as well.</remarks>
    public interface IWrapped<T>: IDisposableEx
    {
        /// <summary>
        /// The original object.
        /// </summary>
        T Value { get; }
    }
}
