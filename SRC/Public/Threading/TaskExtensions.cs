/********************************************************************************
* TaskExtensions.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
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
        public static async Task<TT> Cast<T, TT>(this Task<T> task)
        {
            Ensure.Parameter.IsNotNull(task, nameof(task));

            object result = (await task)!;
            return (TT) result;
        }

        /// <summary>
        /// Casts the given <see cref="Task"/>.
        /// </summary>
        public static Task Cast<T>(this Task<T> task, Type returnType)
        {
            Ensure.Parameter.IsNotNull(task, nameof(task));
            Ensure.Parameter.IsNotNull(returnType, nameof(returnType));

            return (Task) Cache
                .GetOrAdd((typeof(T), returnType), () =>
                {
                    MethodInfo cast = ((MethodCallExpression) ((Expression<Action>) (() => Cast<object, object>(null!))).Body)
                        .Method
                        .GetGenericMethodDefinition();

                    return cast
                        .MakeGenericMethod(typeof(T), returnType)
                        .ToStaticDelegate();
                })
                .Invoke(new object[] { task });
        }
    }
}
