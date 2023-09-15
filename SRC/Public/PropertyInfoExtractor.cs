/********************************************************************************
* PropertyInfoExtractor.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.Primitives
{
    /// <summary>
    /// Extracts the underlying <see cref="PropertyInfo"/> from <see cref="Expression"/>s
    /// </summary>
    public static class PropertyInfoExtractor
    {
        /// <summary>
        /// Extracts the underlying <see cref="PropertyInfo"/> from the given <paramref name="exprression"/>
        /// </summary>
        public static PropertyInfo Extract(LambdaExpression exprression) => (PropertyInfo) ((MemberExpression) exprression.Body).Member;

        /// <summary>
        /// Extracts the underlying <see cref="PropertyInfo"/> from the given <paramref name="exprression"/>
        /// </summary>
        public static PropertyInfo Extract<T>(Expression<Func<T>> exprression) => Extract((LambdaExpression) exprression);

        /// <summary>
        /// Extracts the underlying <see cref="PropertyInfo"/> from the given <paramref name="exprression"/>
        /// </summary>
        public static PropertyInfo Extract<T, TT>(Expression<Func<T, TT>> exprression) => Extract((LambdaExpression) exprression);
    }
}
