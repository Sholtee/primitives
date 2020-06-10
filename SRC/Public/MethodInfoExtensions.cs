/********************************************************************************
* MethodInfoExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.Primitives
{
    /// <summary>
    /// Helper class for creating delegates from <see cref="MethodInfo"/>.
    /// </summary>
    public static class MethodInfoExtensions
    {
        private static IEnumerable<Expression> GetInvocationArguments(MethodBase method, ParameterExpression parameters) => method.GetParameters().Select
        (
            (param, i) => Expression.Convert
            (
                Expression.ArrayAccess(parameters, Expression.Constant(i)),
                param.ParameterType
            )
        );

        /// <summary>
        /// Creates a new instance delegate from the given <see cref="MethodInfo"/>.
        /// </summary>
        public static Func<object, object?[], object> ToInstanceDelegate(this MethodInfo method)
        {
            Ensure.Parameter.IsNotNull(method, nameof(method));

            return Cache.GetOrAdd(method, () =>
            {
                ParameterExpression
                    instance = Expression.Parameter(typeof(object), nameof(instance)),
                    paramz = Expression.Parameter(typeof(object?[]), nameof(paramz));

                Expression call = Expression.Call(
                    Expression.Convert(instance, method.DeclaringType),
                    method,
                    GetInvocationArguments(method, paramz));

                call = method.ReturnType != typeof(void)
                    ? (Expression)Expression.Convert(call, typeof(object))
                    : Expression.Block(typeof(object), call, Expression.Default(typeof(object)));

                return Expression.Lambda<Func<object, object?[], object>>
                (
                    call,
                    instance,
                    paramz
                ).Compile();
            });
        }

        /// <summary>
        /// Creates a new static delegate from the given <see cref="MethodBase"/> that can be either <see cref="ConstructorInfo"/> or <see cref="MethodInfo"/>.
        /// </summary>
        /// <param name="methodBase"></param>
        /// <returns></returns>
        public static Func<object?[], object> ToStaticDelegate(this MethodBase methodBase)
        {
            Ensure.Parameter.IsNotNull(methodBase, nameof(methodBase));

            return Cache.GetOrAdd(methodBase, () =>
            {
                ParameterExpression paramz = Expression.Parameter(typeof(object?[]), nameof(paramz));

                IEnumerable<Expression> arguments = GetInvocationArguments(methodBase, paramz);

                Expression call = methodBase switch
                {
                    ConstructorInfo ctor => Expression.Convert
                    (
                        Expression.New(ctor, arguments),
                        typeof(object)
                    ),
                    MethodInfo voidMethod when voidMethod.ReturnType == typeof(void) => Expression.Block
                    (
                        typeof(object),
                        Expression.Call(voidMethod, arguments),
                        Expression.Default(typeof(object))
                    ),
                    MethodInfo method when method.ReturnType != typeof(void) => Expression.Convert
                    (
                        Expression.Call(method, arguments),
                        typeof(object)
                    ),
                    _ => throw new NotSupportedException() // TODO
                };

                return Expression.Lambda<Func<object?[], object>>
                (
                    call, 
                    paramz
                ).Compile();
            });
        }
    }
}
