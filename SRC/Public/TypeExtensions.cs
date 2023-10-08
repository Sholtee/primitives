/********************************************************************************
* TypeExtensions.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;

namespace Solti.Utils.Primitives
{
    /// <summary>
    /// Defines extensions for the <see cref="Type"/> type.
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// Gets the friendly name of the given type.
        /// </summary>
        public static string GetFriendlyName(this Type src)
        {
            if (src.IsGenericType)
            {
                string 
                    namePrefix = src.FullName.Split(new char[] { '`' }, StringSplitOptions.RemoveEmptyEntries)[0],
                    genericParameters = string.Join(", ", src.GetGenericArguments().Select(GetFriendlyName));

                return namePrefix + "{" + genericParameters + "}";
            }

            return src.FullName ?? src.Name;
        }
    }
}
