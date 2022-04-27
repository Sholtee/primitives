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
    [SimpleJob(RunStrategy.Throughput, targetCount: 15, invocationCount: 300000)]
    public class ConcurrentLinkedList
    {
        public ConcurrentLinkedList<int> List { get; set; }

        [GlobalSetup(Target = nameof(AddFirst))]
        public void SetupAdd() => List = new ConcurrentLinkedList<int>();

        [Benchmark]
        public LinkedListNode<int> AddFirst() => List.AddFirst(0);

        [GlobalSetup(Target = nameof(Enumerate_MoveNext))]
        public void SetupEnumerate()
        {
            List = new();

            for (int i = 0; i < 500; i++)
                List.AddFirst(i);
        }

        [Benchmark(OperationsPerInvoke = 500)]
        public void Enumerate_MoveNext()
        {
            using IEnumerator<int> enumerator = List.GetEnumerator();
            while (enumerator.MoveNext())
            {
                _ = enumerator.Current;
            }
        }
    }
}
