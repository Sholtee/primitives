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
        [Params(true, false)]
        public bool SupportFinalizer { get; set; }

        [Benchmark]
        public void CreateAndDispose()
        {
            using (new Patterns.Disposable(SupportFinalizer)) { }
        }
    }
}
