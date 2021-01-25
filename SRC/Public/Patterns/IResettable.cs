/********************************************************************************
* IResettable.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.Primitives.Patterns
{
    /// <summary>
    /// Describes an object having resettable state.
    /// </summary>
    public interface IResettable 
    {
        /// <summary>
        /// Returns true if the state of the object differs from the default.
        /// </summary>
        bool Dirty { get; }

        /// <summary>
        /// Resets the state of the object
        /// </summary>
        void Reset();
    }
}
