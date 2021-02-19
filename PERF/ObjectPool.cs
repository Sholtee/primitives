/********************************************************************************
* ObjectPool.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using BenchmarkDotNet.Attributes;

namespace Solti.Utils.Primitives.Perf
{
    using static Consts;
    using Threading;

    [MemoryDiagnoser]
    public class ObjectPool
    {
        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void GetAndReturn() 
        {
            using var pool = new ObjectPool<object>(1, () => new object());

            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                pool.Get(CheckoutPolicy.Throw);
                pool.Return();
            }
        }
    }
}
