/********************************************************************************
* ObjectPool.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace Solti.Utils.Primitives.Perf
{
    using Threading;

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 10000000)]
    public class ObjectPool
    {
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

        [Params(true, false)]
        public bool Permissive { get; set; }

        [GlobalSetup(Target = nameof(GetAndReturn))]
        public void Setup()
        {
            OurPool = new ObjectPool<object>(new SimpleLifetimeManager<object>(), PoolConfig.Default with { CheckoutPolicy = CheckoutPolicy.Throw, Permissive = Permissive });
            GC.SuppressFinalize(OurPool);
        }

        public ObjectPool<object> OurPool { get; set; }

        [Benchmark]
        public void GetAndReturn()
        {
            object obj = OurPool.Get();
            OurPool.Return(obj);
        }
    }

    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Throughput, invocationCount: 10000000)]
    public class ThreadLocal
    {
        private readonly System.Threading.ThreadLocal<object> FThreadLocal = new(static () => null);

        private readonly object FValue = new();

        [Benchmark]
        public object GetValue() => FThreadLocal.Value;

        [Benchmark]
        public void SetValue() => FThreadLocal.Value = FValue;
    }
}
