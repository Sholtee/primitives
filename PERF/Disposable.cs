/********************************************************************************
* Disposable.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace Solti.Utils.Primitives.Perf
{
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 10000000)]
    public class Disposable
    {
        [Benchmark]
        public void CreateAndDispose()
        {
            using (new Patterns.Disposable()) { }
        }
    }
}
