/********************************************************************************
* WriteOnce.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Primitives.Patterns
{
    /// <summary>
    /// Represents a variable that can be set only once.
    /// </summary>
    public sealed class WriteOnce: WriteOnce<object> // visszafele kompatibilitas miatt kell
    {
        /// <summary>
        /// Creates a new <see cref="WriteOnce"/> instance.
        /// </summary>
        public WriteOnce(bool strict = true) : base(strict) { }
    }
}
