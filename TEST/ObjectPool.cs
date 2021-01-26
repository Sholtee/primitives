/********************************************************************************
* ObjectPool.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Threading;

using NUnit.Framework;

namespace Solti.Utils.Primitives.Tests
{
    using Patterns;

    [TestFixture]
    public class ObjectPoolThreadingTests
    {
        private class MyObject : IResettable
        {
            public int Value { get; set; }

            public bool Dirty => Value != 0;

            public void Reset() => Value = 0;
        }

        private bool ErrorFound { get; set; }

        private bool Terminated { get; set; }

        private ObjectPool<MyObject> Pool { get; set; }

        [Test]
        public void ThreadingTest([Values(1, 2, 3, 10, 100)] int poolSize) 
        { 
            Pool = new ObjectPool<MyObject>(poolSize, () => new MyObject());
            Terminated = false;
            ErrorFound = false;

            var threads = new Thread[8];
            for (var i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(Run);
            }

            for (var i = 0; i < threads.Length; i++)
            {
                threads[i].Start();
            }

            Thread.Sleep(1000);

            Terminated = true;

            for (var i = 0; i < threads.Length; i++)
            {
                threads[i].Join();
            }

            Assert.IsFalse(ErrorFound);
        }

        private void Run() 
        {
            while (!Terminated)
            {
                using (PoolItem<MyObject> poolItem = Pool.Get())
                {

                    if (poolItem.Value.Value != 0)
                    {
                        ErrorFound = true;
                    }

                    poolItem.Value.Value = 1986;
                }

                using (PoolItem<MyObject> poolItem = Pool.Get())
                {
                    if (poolItem.Value.Value != 0)
                    {
                        ErrorFound = true;
                    }

                    poolItem.Value.Value = 1990;
                }
            }
        }
    }
}
