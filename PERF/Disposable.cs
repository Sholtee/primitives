/********************************************************************************
* Disposable.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using BenchmarkDotNet.Attributes;

namespace Solti.Utils.Primitives.Perf
{
    [MemoryDiagnoser]
    public class Disposable
    {
        [Benchmark]
        public void CreateAndDispose()
        {
            using (new Patterns.Disposable()) { }
        }
    }
}
