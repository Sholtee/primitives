/********************************************************************************
* LinkedList.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace Solti.Utils.Primitives.Perf
{
    using Threading;

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 10000)]
    public class LinkedList
    {
        public ConcurrentLinkedList List { get; set; }

        [GlobalSetup()]
        public void Setup()
        {
            List = new ConcurrentLinkedList();
        }

        [Benchmark]
        public void Add()
        {
            List.Add(new LinkedListNode());
        }
    }
}
