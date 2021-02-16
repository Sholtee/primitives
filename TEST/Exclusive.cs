/********************************************************************************
* Exclusive.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Solti.Utils.Primitives.Threading.Tests
{
    using Properties;

    [TestFixture]
    public class ExclusiveTests
    {
        private Exclusive Exclusive { get; set; }

        [SetUp]
        public void Setup() => Exclusive = new Exclusive();

        [TearDown]
        public void TearDown() => Exclusive?.Dispose();

        [Test]
        public void Acquire_ShouldThrowOnParallelInvocation()
        {
            for (int i = 0; i < 5; i++)
            {
                using (Exclusive.Enter())
                {
                    Assert.ThrowsAsync<InvalidOperationException>(() => Task.Run(Exclusive.Enter), Resources.NOT_EXCLUSIVE);
                }
            }
        }

        [Test]
        public void Acquire_ShouldNotThrowOnSubsequentInvocation()
        {
            for (int i = 0; i < 5; i++)
            {
                using (Exclusive.Enter())
                {
                    using (Exclusive.Enter())
                    {
                        using (Exclusive.Enter())
                        {
                        }
                    }
                }
            }
        }

        [Test]
        public void Acquire_ShouldThrowOnSubsequentParallelInvocation()
        {
            for (int i = 0; i < 5; i++)
            {
                // MethodA()
                using (Exclusive.Enter())
                {
                    // calls MethodB()
                    using (Exclusive.Enter())
                    {
                        // calls MethodC()
                        using (Exclusive.Enter())
                        {
                            // MethodC() is called parallelly
                            Assert.ThrowsAsync<InvalidOperationException>(() => Task.Run(Exclusive.Enter), Resources.NOT_EXCLUSIVE);
                        }
                    }
                }
            }
        }
    }
}
