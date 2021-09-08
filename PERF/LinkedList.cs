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
    public class ConcurrentLinkedList
    {
        public ConcurrentLinkedList<int> List { get; set; }

        [Params(1, 10, 20, 100)]
        public int Count { get; set; }

        [GlobalSetup(Target = nameof(AddFirst))]
        public void SetupAdd()
        {
            List = new ConcurrentLinkedList<int>();
        }

        [Benchmark]
        public void AddFirst()
        {
            for (int i = 0; i < Count; i++)
                List.AddFirst(0);
        }

        [GlobalSetup(Target = nameof(UsingTheEnumerator))]
        public void SetupEnumerator()
        {
            List = new();

            for (int i = 0; i < Count; i++)
                List.AddFirst(i);
        }

        [Benchmark]
        public void UsingTheEnumerator()
        {
            using IEnumerator<int> enumerator = List.GetEnumerator();
            while (enumerator.MoveNext())
            {
                _ = enumerator.Current;
            }
        }

        [GlobalSetup(Target = nameof(UsingForEach))]
        public void SetupForEach()
        {
            List = new();

            for (int i = 0; i < Count; i++)
                List.AddFirst(i);
        }

        [Benchmark]
        public void UsingForEach()
        {
            foreach (int x in List)
            {
                _ = x;
            }
        }
    }
}
