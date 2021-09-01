/********************************************************************************
* Composite.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace Solti.Utils.Primitives.Perf
{
    using Patterns;

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 1000)]
    public class Composite
    {
        public interface IMyComposite : IComposite<IMyComposite>, INotifyOnDispose { }

        public class MyComposite : Composite<IMyComposite>, IMyComposite
        {
            public MyComposite(IMyComposite parent = null) : base(parent) { }
        }

        private IMyComposite Root { get; set; }

        [GlobalSetup(Target = nameof(Children_Add))]
        public void Setup_Children_Add()
        {
            Root = new MyComposite();
        }

        [GlobalCleanup(Target = nameof(Children_Add))]
        public void Cleanup_Children_Add()
        {
            Root?.Dispose();
        }

        [Benchmark]
        public MyComposite Children_Add() => new MyComposite(Root);
/*
        public ConcurrentDictionary<object, byte> Dict { get; set; }

        [GlobalSetup(Target = nameof(ConcurrentDictionary_Add))]
        public void SetupConcurrentDictionary_Add()
        {
            Dict = new();
        }

        [Benchmark]
        public void ConcurrentDictionary_Add()
        {
            Dict.TryAdd(new object(), 0);
        }
*/
    }

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 1000)]
    public class Composite_Dispatch
    {
        private interface IMyComposite : IComposite<IMyComposite>, INotifyOnDispose
        {
            void Foo(string arg);
        }

        private class MyComposite : Composite<IMyComposite>, IMyComposite
        {
            public MyComposite(IMyComposite parent = null) : base(parent) { }
            void IMyComposite.Foo(string arg) => Dispatch(i => i.Foo(arg));
        }

        [Params(0, 1, 2)]
        public int MaxDegreeOfParallelism { get; set; }

        [Params(1, 2)]
        public int Depth { get; set; }

        [Params(1, 2, 5)]
        public int ChildCount { get; set; }

        private IMyComposite Root { get; set; }

        [GlobalSetup(Target = nameof(Dispatch))]
        public void Setup() 
        {
            MyComposite.MaxDegreeOfParallelism = MaxDegreeOfParallelism;

            Root = new MyComposite();

            AddChildren(Root, 0);

            void AddChildren(IMyComposite current, int currentDepth)
            {
                for (int i = 0; i < ChildCount; i++)
                {
                    IMyComposite child = new MyComposite(current);

                    if (currentDepth < Depth)
                        AddChildren(child, currentDepth + 1);
                }
            }
        }

        [Benchmark]
        public void Dispatch() => Root.Foo("cica");

        [GlobalCleanup(Target = nameof(Dispatch))]
        public void Cleanup()
        {
            Root.Dispose();
            Root = null;
        }
    }
}
