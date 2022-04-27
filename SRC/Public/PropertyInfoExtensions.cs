/********************************************************************************
* PropertyInfoExtensions.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Reflection;

namespace Solti.Utils.Primitives
{
    /// <summary>
    /// Helper class for creating delegates from <see cref="PropertyInfo"/>.
    /// </summary>
    public static class PropertyInfoExtensions
    {
        /// <summary>
        /// Creates a getter from the given <see cref="PropertyInfo"/>.
        /// </summary>
        public static InstanceMethod ToGetter(this PropertyInfo src)
        {
            Ensure.Parameter.IsNotNull(src, nameof(src));

            return (src.GetMethod ?? throw new NotSupportedException()).ToInstanceDelegate();
        }

        /// <summary>
        /// Creates a setter from the given <see cref="PropertyInfo"/>.
        /// </summary>
        public static InstanceMethod ToSetter(this PropertyInfo src)
        {
            Ensure.Parameter.IsNotNull(src, nameof(src));

            return (src.SetMethod ?? throw new NotSupportedException()).ToInstanceDelegate();
        }
    }
}
