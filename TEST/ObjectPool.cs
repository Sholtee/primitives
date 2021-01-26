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

namespace Solti.Utils.Primitives.Patterns.Tests
{
    using Patterns;
    using Properties;

    [TestFixture]
    public class ObjectPoolTests 
    {
        [Test]
        public void Get_ShouldReturnTheSameObject() 
        {
            using var pool = new ObjectPool<object>(1, () => new object());

            object
                a, b;

            using (PoolItem<object> item = pool.Get()) 
            {
                a = item.Value;
            }

            using (PoolItem<object> item = pool.Get())
            {
                b = item.Value;
            }

            Assert.AreSame(a, b);
        }

        [Test]
        public void Get_ShouldThrowIfThereIsNoMoreSpaceInThePool() 
        {
            using var pool = new ObjectPool<object>(1, () => new object());

            using (pool.Get(CheckoutPolicy.Throw))
            {
                Assert.Throws<InvalidOperationException>(() => pool.Get(CheckoutPolicy.Throw), Resources.POOL_SIZE_REACHED);
            }
        }

        [Test]
        public void Get_ShouldBlockIfThereIsNoMoreSpaceInThePool() 
        {
            using var pool = new ObjectPool<object>(1, () => new object());

            var evt = new ManualResetEventSlim();

            Task.Run(() => 
            {
                using (pool.Get())
                {
                    evt.Wait();
                }       
            });

            Thread.Sleep(50);

            Assert.False
            (
                Task.Run(() =>
                {
                    using (pool.Get(CheckoutPolicy.Block))
                    {
                    }
                }).Wait(10)
            );

            evt.Set();

            Assert.True
            (
                Task.Run(() =>
                {
                    using (pool.Get(CheckoutPolicy.Block))
                    {
                    }
                }).Wait(10)
            );
        }

        [Test]
        public void Return_ShouldResetTheState() 
        {
            var mockResettable = new Mock<IResettable>(MockBehavior.Strict);
            mockResettable.Setup(r => r.Reset());

            using var pool = new ObjectPool<IResettable>(1, () => mockResettable.Object);

            using (pool.Get()) { }

            mockResettable.Verify(r => r.Reset(), Times.Once);
        }

        [Test]
        public void Dispose_ShouldDisposeTheCreatedObjects([Values(true, false)] bool returned) 
        {
            var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
            mockDisposable.Setup(d => d.Dispose());

            using (var pool = new ObjectPool<IDisposable>(1, () => mockDisposable.Object))
            {
                if (returned)
                    using (pool.Get()) { }
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