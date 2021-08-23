/********************************************************************************
* LinkedList.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace Solti.Utils.Primitives.Perf
{
    using Threading;

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 3000)]
    public class LinkedList
    {
        public ConcurrentLinkedList<int> List { get; set; }

        [Params(1, 10, 20, 100)]
        public int Count { get; set; }

        [GlobalSetup(Target = nameof(Add))]
        public void SetupAdd()
        {
            List = new ConcurrentLinkedList<int>();
        }

        [Benchmark]
        public void Add()
        {
            for (int i = 0; i < Count; i++)
                List.Add(new LinkedListNode<int>());
        }

        [GlobalSetup(Target = nameof(UsingTheEnumerator))]
        public void SetupEnumerator()
        {
            List = new();

            for (int i = 0; i < Count; i++)
                List.Add(new LinkedListNode<int> { Value = i });
        }

        [Benchmark]
        public void UsingTheEnumerator()
        {
            using IEnumerator<LinkedListNode<int>> enumerator = List.GetEnumerator();
            while (enumerator.MoveNext())
            {
                _ = enumerator.Current.Value;
            }
        }

        [GlobalSetup(Target = nameof(UsingForEach))]
        public void SetupForEach()
        {
            List = new();

            for (int i = 0; i < Count; i++)
                List.Add(new LinkedListNode<int> { Value = i });
        }

        [Benchmark]
        public void UsingForEach()
        {
            foreach (LinkedListNode<int> node in List)
            {
                _ = node.Value;
            }
        }
    }
}
