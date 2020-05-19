/********************************************************************************
* Ensure.cs                                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Runtime.CompilerServices;

namespace Solti.Utils.Primitives
{
    using Properties;

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
        }

        public static class Type<T> where T: class
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void IsInterface() 
            {
                if (!typeof(T).IsInterface)
                {
                    var ex = new InvalidOperationException(Resources.NOT_AN_INTERFACE);
                    ex.Data["Type"] = typeof(T);

                    throw ex;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void IsSupportedBy(object obj) 
            {
                if (obj as T == null)
                    throw new NotSupportedException(string.Format(Resources.Culture, Resources.INTERFACE_NOT_SUPPORTED, typeof(T)));
            }
        }
    }
}
