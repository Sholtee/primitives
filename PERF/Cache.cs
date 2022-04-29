/********************************************************************************
* Cache.cs                                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using BenchmarkDotNet.Attributes;

namespace Solti.Utils.Primitives.Perf
{
    [MemoryDiagnoser]
    public class Cache
    {
        private const int OperationsPerInvoke = 10000;

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void GetOrAdd()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                Primitives.Cache.GetOrAdd(i, _ => new object());
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void GetOrAddSlim()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                Primitives.CacheSlim.GetOrAdd(i, _ => new object());
            }
        }
    }
}
