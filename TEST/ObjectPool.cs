﻿/********************************************************************************
* ObjectPool.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.Primitives.Threading.Tests
{
    using Patterns;
    using Properties;
    using Threading;

    [TestFixture]
    public class ObjectPoolTests 
    {
        [Test]
        public void Get_ShouldReturnTheSameObjectIfThePoolIsNotPermissive() 
        {
            using var pool = new ObjectPool<object>(() => new object(), PoolConfig.Default with { Capacity = 2, Permissive = false });

            Assert.AreEqual(pool.Get(), pool.Get());
            Assert.That(pool.Count, Is.EqualTo(1));
        }

        [Test]
        public void Get_ShouldNotReturnTheSameObjectIfThePoolIsPermissive()
        {
            using var pool = new ObjectPool<object>(() => new object(), PoolConfig.Default with { Capacity = 2, Permissive = true });

            Assert.AreNotEqual(pool.Get(), pool.Get());
            Assert.That(pool.Count, Is.EqualTo(2));
        }

        [Test]
        public void Return_ShouldThrowIfTheCheckinNotAllowed()
        {
            using var pool = new ObjectPool<object>(() => new object(), PoolConfig.Default with { Permissive = false });

            object obj = null;

            Task.Factory.StartNew(() => obj = pool.Get()).Wait();

            Assert.Throws<InvalidOperationException>(() => pool.Return(obj));
        }

        [Test]
        public void Get_ShouldReturnTheSameObjectInTheSameThread([Values(1, 2, 3)] int capacity)
        {
            using var pool = new ObjectPool<object>(() => new object(), PoolConfig.Default with { Capacity = capacity });

            Assert.AreSame(pool.Get(), pool.Get());
        }

        [Test]
        public void Get_ShouldRevertTheCheckoutProcessIfTheFactoryThrows() 
        {
            using var pool = new ObjectPool<object>(() => throw new Exception(), PoolConfig.Default with { Capacity = 2, CheckoutPolicy = CheckoutPolicy.Block });

            Assert.Throws<Exception>(() => pool.Get());
            Assert.That(pool, Is.Empty);
        }

        [Test]
        public void Get_ShouldThrowIfThereIsNoMoreSpaceInThePool() 
        {
            using var pool = new ObjectPool<object>(() => new object(), PoolConfig.Default with { Capacity = 1, CheckoutPolicy = CheckoutPolicy.Throw });

            using (pool.GetItem())
            {
                Assert.DoesNotThrowAsync(() => Task.Run(() => Assert.Throws<InvalidOperationException>(() => pool.Get(), Resources.MAX_SIZE_REACHED)));
            }
        }

        [Test]
        public void Get_ShouldReturnNullIfThereIsNoMoreSpaceInThePool()
        {
            using var pool = new ObjectPool<object>(() => new object(), PoolConfig.Default with { Capacity = 1, CheckoutPolicy = CheckoutPolicy.Discard });

            using (pool.GetItem())
            {
                Assert.DoesNotThrowAsync(() => Task.Run(() => Assert.IsNull(pool.Get())));
            }
        }

        [Test]
        public void Get_ShouldBlockIfThereIsNoMoreSpaceInThePool() 
        {
            using var pool = new ObjectPool<object>(() => new object(), PoolConfig.Default with { Capacity = 1, CheckoutPolicy = CheckoutPolicy.Block });

            var evt = new ManualResetEventSlim();

            Task.Run(() => 
            {
                using (pool.GetItem())
                {
                    evt.Wait();
                }       
            });

            Thread.Sleep(50);

            Assert.False
            (
                Task.Run(() =>
                {
                    using (pool.GetItem())
                    {
                    }
                }).Wait(10)
            );

            evt.Set();

            Assert.True
            (
                Task.Run(() =>
                {
                    using (pool.GetItem())
                    {
                    }
                }).Wait(10)
            );
        }

        [Test]
        public void Return_ShouldResetTheState() 
        {
            bool dirty = true;

            var mockResettable = new Mock<IResettable>(MockBehavior.Strict);
            mockResettable
                .Setup(r => r.Reset())
                .Callback(() => dirty = false);
          
            mockResettable
                .SetupGet(r => r.Dirty)
                .Returns(() => dirty);

            using var pool = new ObjectPool<IResettable>(() => mockResettable.Object);

            using (pool.GetItem()) 
            {          
            }

            mockResettable.Verify(r => r.Reset(), Times.Once);
        }

        [Test]
        public void Return_ShouldThrowIfTheStateCouldNotBeReverted()
        {
            var mockResettable = new Mock<IResettable>(MockBehavior.Strict);
            mockResettable.Setup(r => r.Reset());

            mockResettable
                .SetupGet(r => r.Dirty)
                .Returns(true);

            using var pool = new ObjectPool<IResettable>(() => mockResettable.Object);

            IResettable val = pool.Get();
            Assert.Throws<InvalidOperationException>(() => pool.Return(val), Resources.RESET_FAILED);
        }

        [Test]
        public void Dispose_ShouldDisposeTheCreatedObjects([Values(true, false)] bool returned) 
        {
            var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
            mockDisposable.Setup(d => d.Dispose());

            using (var pool = new ObjectPool<IDisposable>(() => mockDisposable.Object))
            {
                if (returned)
                    using (pool.GetItem()) { }
                else
                    pool.Get();
            }

            mockDisposable.Verify(d => d.Dispose(), Times.Once);
        }
    }

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
        public void ThreadingTest([Values(1, 2, 3, 10, 100)] int capacity) 
        { 
            Pool = new ObjectPool<MyObject>(() => new MyObject(), PoolConfig.Default with { Capacity = capacity });
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
                using (PoolItem<MyObject> poolItem = Pool.GetItem())
                {

                    if (poolItem.Value.Value != 0)
                    {
                        ErrorFound = true;
                    }

                    poolItem.Value.Value = 1986;
                }

                using (PoolItem<MyObject> poolItem = Pool.GetItem())
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
