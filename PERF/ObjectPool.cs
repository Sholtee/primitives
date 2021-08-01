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

        [GlobalSetup(Target = nameof(MS_Extensions_ObjectPool_GetAndReturn))]
        public void Setup_TheirPool()
        {
            TheirPool = new DefaultObjectPool<object>(new SimplePoolPolicy());
            GC.SuppressFinalize(TheirPool);
        }

        public DefaultObjectPool<object> TheirPool { get; set; }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void MS_Extensions_ObjectPool_GetAndReturn()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                object obj = TheirPool.Get();
                TheirPool.Return(obj);
            }
        }

        private sealed class SimpleLifetimeManager<T> : ILifetimeManager<T> where T: class, new()
        {
            public T Create() => new T();

            public void Dispose(T item)
            {
            }

            public void CheckOut(T item)
            {
            }

            public void CheckIn(T item)
            {
            }
        }

        [GlobalSetup(Target = nameof(Solti_Utils_ObjectPool_GetAndReturn))]
        public void Setup_OurPool()
        {
            OurPool = new ObjectPool<object>(1, new SimpleLifetimeManager<object>());
            GC.SuppressFinalize(OurPool);
        }

        public ObjectPool<object> OurPool { get; set; }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void Solti_Utils_ObjectPool_GetAndReturn()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                OurPool.Get(CheckoutPolicy.Throw);
                OurPool.Return();
            }
        }
    }
}
