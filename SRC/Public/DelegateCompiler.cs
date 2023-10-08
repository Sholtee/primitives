/********************************************************************************
* DelegateCompiler.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Solti.Utils.Primitives
{
    using Properties;

    /// <summary>
    /// Represents a <see cref="System.Delegate"/> that is compiled in the future.
    /// </summary>
    /// <typeparam name="TDelegate"></typeparam>
    public sealed class FutureDelegate<TDelegate> where TDelegate : Delegate
    {
        private TDelegate? FDelegate;

        /// <summary>
        /// The compiled delegate.
        /// </summary>
        public TDelegate Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => FDelegate ?? throw new InvalidOperationException(Resources.NOT_COMPILED);
            internal set => FDelegate = value;
        }

        /// <summary>
        /// Returns true if the <see cref="Value"/> contains a compiled delegate, false otherwise.
        /// </summary>
        public bool IsCompiled
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => FDelegate is not null;
        }

    }

    /// <summary>
    /// Represents a batched delegate compiler.
    /// </summary>
    public sealed class DelegateCompiler
    {
        private readonly List<Expression> FAssigns = new();

        /// <summary>
        /// Registers a new delegate to be compiled.
        /// </summary>
        public FutureDelegate<TDelegate> Register<TDelegate>(Expression<TDelegate> lambda) where TDelegate : Delegate
        {
            FutureDelegate<TDelegate> result = new();

            FAssigns.Add
            (
                Expression.Assign
                (
                    Expression.Property
                    (
                        Expression.Constant(result),
                        PropertyInfoExtractor.Extract(() => result.Value)
                    ),
                    lambda ?? throw new ArgumentNullException(nameof(lambda))
                )
            );

            return result;
        }

        /// <summary>
        /// Compiles all the registered delegates at once.
        /// </summary>
        public void Compile()
        {
            if (FAssigns.Count is 0)
                return;

            Expression<Action> expr = Expression.Lambda<Action>
            (
                Expression.Block
                (
                    FAssigns
                )
            );

            Debug.WriteLine(expr.GetDebugView());
            expr.Compile().Invoke();
            FAssigns.Clear();
        }
    }
}
