/********************************************************************************
* RedBlackTree.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace Solti.Utils.Primitives.Perf
{
    using Patterns;

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 300)]
    public class RedBlackTree
    {
        private sealed class IntComparer : Singleton<IntComparer>, IComparer<int>
        {
            public int Compare(int x, int y) => x.CompareTo(y);
        }

        public RedBlackTree<int> Tree { get; set; }

        [GlobalSetup(Target = nameof(Add))]
        public void SetupAdd() => Tree = new RedBlackTree<int>(IntComparer.Instance);

        [Benchmark(OperationsPerInvoke = 10000)]
        public void Add()
        {
            for (int i = 0; i < 10000; i++)
                Tree.Add(i);
        }

        [GlobalSetup(Target = nameof(With))]
        public void SetupWith() => Tree = new RedBlackTree<int>(IntComparer.Instance);

        [Benchmark(OperationsPerInvoke = 500)]
        public void With()
        {
            RedBlackTree<int> tree = Tree;
            for (int i = 0; i < 500; i++)
                tree = tree.With(i);
        }

        [GlobalSetup(Target = nameof(Enumerate_MoveNext))]
        public void SetupEnumerate()
        {
            Tree = new RedBlackTree<int>(IntComparer.Instance);

            for (int i = 0; i < 100000; i++)
                Tree.Add(i);
        }

        [Benchmark(OperationsPerInvoke = 100000)]
        public void Enumerate_MoveNext()
        {
            using IEnumerator<RedBlackTreeNode<int>> enumerator = Tree.GetEnumerator();
            while (enumerator.MoveNext())
            {
                _ = enumerator.Current;
            }
        }
    }
}
