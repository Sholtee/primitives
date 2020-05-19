/********************************************************************************
* Ensure.cs                                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Solti.Utils.Primitives
{
    using Properties;

    /// <summary>
    /// Every method that can be accessed publicly or use one or more parameters that were passed outside of the library 
    /// should use this class for basic validations to ensure consistent validation errors.
    /// </summary>
    [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types")]
    public static class Ensure
    {
        /// <summary>
        /// Parameter related validations.
        /// </summary>
        public static class Parameter
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T IsNotNull<T>(T? argument, string name) where T : class =>
                argument ?? throw new ArgumentNullException(name);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T? IsNotNull<T>(T? argument, string name) where T : struct =>
                argument ?? throw new ArgumentNullException(name);
        }

        /// <summary>
        /// Type related validations.
        /// </summary>
        /// <typeparam name="T"></typeparam>
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
