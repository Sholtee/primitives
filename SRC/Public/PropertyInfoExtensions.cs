/********************************************************************************
* PropertyInfoExtensions.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.Primitives
{
    /// <summary>
    /// Helper class for creating delegates from <see cref="PropertyInfo"/>.
    /// </summary>
    public static class PropertyInfoExtensions // TODO: indexer property support
    {
        /// <summary>
        /// Creates a getter from the given <see cref="PropertyInfo"/>.
        /// </summary>
        public static Func<object, object?> ToGetter(this PropertyInfo src)
        {
            Ensure.Parameter.IsNotNull(src, nameof(src));

            return Cache.GetOrAdd(src, static src =>
            {
                ParameterExpression p = Expression.Parameter(typeof(object), "instance");

                return Expression
                    .Lambda<Func<object, object?>>
                    (
                        Expression.Convert // cast-olas boxibf miatt kell (ha a property visszaterese ValueType)
                        (
                            Expression.Property
                            (
                                Expression.Convert(p, src.ReflectedType), src
                            ), 
                            typeof(object)
                        ), 
                        p
                    )
                    .Compile();
            });
        }

        /// <summary>
        /// Creates a setter from the given <see cref="PropertyInfo"/>.
        /// </summary>
        public static Action<object, object?> ToSetter(this PropertyInfo src)
        {
            Ensure.Parameter.IsNotNull(src, nameof(src));

            return Cache.GetOrAdd(src, static src =>
            {
                ParameterExpression
                    inst = Expression.Parameter(typeof(object), "instance"),
                    val  = Expression.Parameter(typeof(object), "value");

                return Expression
                    .Lambda<Action<object, object?>>
                    (
                        Expression.Assign
                        (
                            Expression.Property
                            (
                                Expression.Convert(inst, src.ReflectedType), 
                                src
                            ),
                            Expression.Convert(val, src.PropertyType)
                        ),
                        inst,
                        val
                    )
                    .Compile();
            });
        }
    }
}
