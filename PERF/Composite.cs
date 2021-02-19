/********************************************************************************
* Composite.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.Primitives.Perf
{
    using Patterns;

    using static Consts;

    [MemoryDiagnoser]
    public class Composite
    {
        private interface IMyComposite : IComposite<IMyComposite> { }

        private class MyComposite : Composite<IMyComposite>, IMyComposite
        {
            public MyComposite() : base(null)
            {
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Children_Add() 
        {
            IMyComposite root = new MyComposite(); // direkt nincs using h a Dispose() hivas ne szamitson bele a meresbe
            GC.SuppressFinalize(root);

            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                IMyComposite child = new MyComposite();
                GC.SuppressFinalize(child);

                root.Children.Add(child);
            }
        }
    }

    [MemoryDiagnoser]
    public class Composite_Dispatch
    {
        private interface IMyComposite : IComposite<IMyComposite> 
        {
            void Foo(string arg);
        }

        private class MyComposite : Composite<IMyComposite>, IMyComposite
        {
            public MyComposite() : base(null)
            {
            }

            void IMyComposite.Foo(string arg) => Dispatch(null, arg);
        }

        [Params(0, 1, 2, 4)]
        public int MaxDegreeOfParallelism { get; set; }

        [Params(1, 2, 3)]
        public int Depth { get; set; }

        [Params(1, 2, 5)]
        public int ChildCount { get; set; }

        private IMyComposite Root { get; set; }

        [GlobalSetup(Target = nameof(Dispatch))]
        public void Setup() 
        {
            MyComposite.MaxDegreeOfParallelism = MaxDegreeOfParallelism;

            Root = new MyComposite();
            GC.SuppressFinalize(Root);

            AddChildren(Root, 0);

            void AddChildren(IMyComposite current, int currentDepth)
            {
                for (int i = 0; i < ChildCount; i++)
                {
                    IMyComposite child = new MyComposite();
                    GC.SuppressFinalize(child);

                    if (currentDepth < Depth)
                        AddChildren(child, currentDepth + 1);

                    current.Children.Add(child);
                }
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Dispatch()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                Root.Foo("cica");
            }
        }
    }
}
