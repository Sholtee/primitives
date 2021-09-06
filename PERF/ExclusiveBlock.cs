/********************************************************************************
* ExclusiveBlock.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using BenchmarkDotNet.Attributes;

namespace Solti.Utils.Primitives.Perf
{
    using Threading;

    [MemoryDiagnoser]
    public class ExclusiveBlock
    {
        public Threading.ExclusiveBlock Block { get; set; }

        [Params(ExclusiveBlockFeatures.None, ExclusiveBlockFeatures.SupportsRecursion)]
        public ExclusiveBlockFeatures Features { get; set; }

        [GlobalSetup]
        public void Setup() => Block = new(Features);

        [GlobalCleanup]
        public void Cleanup() => Block?.Dispose();

        [Benchmark]
        public void EnterAndLeave()
        {
            using (Block.Enter())
            {
                _ = Block.Features;
            }
        }
    }
}
