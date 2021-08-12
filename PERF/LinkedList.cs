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
        public ConcurrentLinkedList<int> List { get; set; }

        [GlobalSetup(Target = nameof(Add))]
        public void SetupAdd()
        {
            List = new ConcurrentLinkedList<int>();
        }

        [Benchmark]
        public void Add()
        {
            List.Add(new LinkedListNode<int>());
        }

        [GlobalSetup(Target = nameof(ForEach))]
        public void SetupForEach()
        {
            List = new ConcurrentLinkedList<int>();
            List.Add(new LinkedListNode<int>());
        }

        [Benchmark]
        public void ForEach()
        {
            foreach (LinkedListNode<int> node in List)
            {
            }
        }
    }
}
