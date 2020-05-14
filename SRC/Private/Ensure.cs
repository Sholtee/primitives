/********************************************************************************
* Ensure.cs                                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Runtime.CompilerServices;

namespace Solti.Utils.Primitives
{
    /// <summary>
    /// Every method that can be accessed publicly or use one or more parameters that were passed outside of the library 
    /// should use this class for basic validations to ensure consistent validation errors.
    /// </summary>
    internal static class Ensure
    {
        public static class Parameter
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T IsNotNull<T>(T? argument, string name) where T : class =>
                argument ?? throw new ArgumentNullException(name);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T? IsNotNull<T>(T? argument, string name) where T : struct =>
                argument ?? throw new ArgumentNullException(name);
        }
    }
}
