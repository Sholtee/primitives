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

        [GlobalSetup(Target = nameof(Add))]
        public void SetupAdd()
        {
            List = new ConcurrentLinkedList();
        }

        [Benchmark]
        public void Add()
        {
            List.Add(new LinkedListNode());
        }

        [GlobalSetup(Target = nameof(ForEach))]
        public void SetupForEach()
        {
            List = new ConcurrentLinkedList();
            List.Add(new LinkedListNode());
        }

        [Benchmark]
        public void ForEach()
        {
            foreach (LinkedListNode node in List)
            {
            }
        }
    }
}
