/********************************************************************************
* TaskExtensions.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Solti.Utils.Primitives.Threading
{
    /// <summary>
    /// Exposes several handy methods related to <see cref="Task"/>s.
    /// </summary>
    public static class TaskExtensions
    {
        /// <summary>
        /// Casts the given <see cref="Task"/>.
        /// </summary>
        [SuppressMessage("Reliability", "CA2008:Do not create tasks without passing a TaskScheduler")]
        public static Task<TT> Cast<T, TT>(this Task<T> task)
        {
            Ensure.Parameter.IsNotNull(task, nameof(task));

            return task.ContinueWith(t => 
            {
                object result = t.Result!;
                return Task.FromResult((TT) result);
            }).Unwrap();
        }

        /// <summary>
        /// Casts the given <see cref="Task"/>.
        /// </summary>
        public static Task Cast<T>(this Task<T> task, Type returnType) => (Task) Cache
            .GetOrAdd(returnType, () =>
            {
                MethodInfo cast = ((MethodCallExpression) ((Expression<Action>) (() => Cast<object, object>(null!))).Body)
                    .Method
                    .GetGenericMethodDefinition();

                return cast.MakeGenericMethod(typeof(T), returnType).ToStaticDelegate();
            })
            .Invoke(new object[] { task });
    }
}
