/********************************************************************************
* ConstructorInfoExtractor.cs                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.Primitives
{
    /// <summary>
    /// Extracts the underlying <see cref="ConstructorInfo"/> from <see cref="Expression"/>s
    /// </summary>
    public static class ConstructorInfoExtractor
    {
        /// <summary>
        /// Extracts the underlying <see cref="ConstructorInfo"/> from the given <paramref name="exprression"/>
        /// </summary>
        public static ConstructorInfo Extract(LambdaExpression exprression) => ((NewExpression) exprression.Body).Constructor;

        /// <summary>
        /// Extracts the underlying <see cref="ConstructorInfo"/> from the given <paramref name="exprression"/>
        /// </summary>
        public static ConstructorInfo Extract<T>(Expression<Func<T>> exprression) => Extract((LambdaExpression) exprression);
    }
}
