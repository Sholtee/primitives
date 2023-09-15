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
        /// Extracts the underlying <see cref="PropertyInfo"/> from the given <paramref name="expression"/>
        /// </summary>
        public static PropertyInfo Extract(LambdaExpression expression) => (PropertyInfo) ((MemberExpression) expression.Body).Member;

        /// <summary>
        /// Extracts the underlying <see cref="PropertyInfo"/> from the given <paramref name="expression"/>
        /// </summary>
        public static PropertyInfo Extract<T>(Expression<Func<T>> expression) => Extract((LambdaExpression) expression);

        /// <summary>
        /// Extracts the underlying <see cref="PropertyInfo"/> from the given <paramref name="expression"/>
        /// </summary>
        public static PropertyInfo Extract<T, TT>(Expression<Func<T, TT>> expression) => Extract((LambdaExpression) expression);
    }
}
