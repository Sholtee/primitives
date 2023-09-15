/********************************************************************************
* MethodInfoExtractor.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.Primitives
{
    /// <summary>
    /// Extracts the underlying <see cref="MethodInfo"/> from <see cref="Expression"/>s
    /// </summary>
    public static class MethodInfoExtractor
    {
        /// <summary>
        /// Extracts the underlying <see cref="MethodInfo"/> from the given <paramref name="exprression"/>
        /// </summary>
        public static MethodInfo Extract(LambdaExpression exprression) => ((MethodCallExpression) exprression.Body).Method;

        /// <summary>
        /// Extracts the underlying <see cref="MethodInfo"/> from the given <paramref name="exprression"/>
        /// </summary>
        public static MethodInfo Extract(Expression<Action> exprression) => Extract((LambdaExpression) exprression);

        /// <summary>
        /// Extracts the underlying <see cref="MethodInfo"/> from the given <paramref name="exprression"/>
        /// </summary>
        public static MethodInfo Extract<T>(Expression<Action<T>> exprression) => Extract((LambdaExpression) exprression);
    }
}
