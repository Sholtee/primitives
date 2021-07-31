/********************************************************************************
* ObjectPool.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.ObjectPool;

namespace Solti.Utils.Primitives.Perf
{
    using static Consts;
    using Threading;

    [MemoryDiagnoser]
    public class ObjectPool_Comparison
    {
        private sealed class SimplePoolPolicy : IPooledObjectPolicy<object>
        {
            public object Create() => new object();

            public bool Return(object obj) => true;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void MS_Extensions_ObjectPool_GetAndReturn()
        {
            var pool = new DefaultObjectPool<object>(new SimplePoolPolicy());

            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                object obj = pool.Get();
                pool.Return(obj);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Solti_Utils_ObjectPool_GetAndReturn()
        {
            var pool = new ObjectPool<object>(1, () => new object(), suppressItemDispose: true);
            GC.SuppressFinalize(pool);

            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                object obj = pool.Get(CheckoutPolicy.Throw);
                pool.Return();
            }
        }
    }
}
