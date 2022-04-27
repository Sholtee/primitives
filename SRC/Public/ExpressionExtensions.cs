/********************************************************************************
* ExpressionExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.Primitives
{
    /// <summary>
    /// Contains some <see cref="Expression"/> related helper methods.
    /// </summary>
    public static class ExpressionExtensions
    {
        private static readonly InstanceMethod GetDebugViewCore = typeof(Expression)
            .GetProperty("DebugView", BindingFlags.Instance | BindingFlags.NonPublic) // DebugView property internal, tudja fasz miert
            .ToGetter();

        /// <summary>
        /// Gets the debug view of the given <paramref name="expression"/>.
        /// </summary>
        public static string GetDebugView(this Expression expression) => (string) GetDebugViewCore(expression ?? throw new ArgumentNullException(nameof(expression)))!;
    }
}
