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
        /// Extracts the underlying <see cref="MethodInfo"/> from the given <paramref name="expression"/>
        /// </summary>
        public static MethodInfo Extract(LambdaExpression expression) => ((MethodCallExpression) expression.Body).Method;

        /// <summary>
        /// Extracts the underlying <see cref="MethodInfo"/> from the given <paramref name="expression"/>
        /// </summary>
        public static MethodInfo Extract(Expression<Action> expression) => Extract((LambdaExpression) expression);

        /// <summary>
        /// Extracts the underlying <see cref="MethodInfo"/> from the given <paramref name="expression"/>
        /// </summary>
        public static MethodInfo Extract<T>(Expression<Action<T>> expression) => Extract((LambdaExpression) expression);

        /// <summary>
        /// Extracts the underlying <see cref="MethodInfo"/> from the given <paramref name="expression"/>
        /// </summary>
        public static MethodInfo Extract<T, TT>(Expression<Action<T, TT>> expression) => Extract((LambdaExpression) expression);
    }
}
