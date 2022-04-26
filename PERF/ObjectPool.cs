/********************************************************************************
* ObjectPool.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

using Microsoft.Extensions.ObjectPool;

namespace Solti.Utils.Primitives.Perf
{
    using Threading;

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 10000000)]
    public class ObjectPool_Comparison
    {
        private sealed class SimplePoolPolicy : IPooledObjectPolicy<object>
        {
            public object Create() => new object();

            public bool Return(object obj) => true;
        }

        [GlobalSetup(Target = nameof(MS_Extensions_ObjectPool_GetAndReturn))]
        public void Setup_TheirPool()
        {
            TheirPool = new DefaultObjectPool<object>(new SimplePoolPolicy());
            GC.SuppressFinalize(TheirPool);
        }

        public DefaultObjectPool<object> TheirPool { get; set; }

        [Benchmark]
        public void MS_Extensions_ObjectPool_GetAndReturn()
        {
            object obj = TheirPool.Get();
            TheirPool.Return(obj);
        }

        private sealed class SimpleLifetimeManager<T> : ILifetimeManager<T> where T: class, new()
        {
            public T Create() => new();

            public void Dispose(T item)
            {
            }

            public void CheckOut(T item)
            {
            }

            public void CheckIn(T item)
            {
            }

            public void RecursionDetected()
            {
            }
        }

        [GlobalSetup(Target = nameof(Solti_Utils_ObjectPool_GetAndReturn))]
        public void Setup_OurPool()
        {
            OurPool = new ObjectPool<object>(new SimpleLifetimeManager<object>());
            GC.SuppressFinalize(OurPool);
        }

        public ObjectPool<object> OurPool { get; set; }

        [Benchmark]
        public void Solti_Utils_ObjectPool_GetAndReturn()
        {
            object obj = OurPool.Get(CheckoutPolicy.Throw);
            OurPool.Return(obj);
        }
    }
}
