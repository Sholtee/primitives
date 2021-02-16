/********************************************************************************
* ExclusiveBlock.cs                                                             *
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
    public class ExclusiveBlockTests
    {
        private ExclusiveBlock ExclusiveBlock { get; set; }

        [SetUp]
        public void Setup() => ExclusiveBlock = new ExclusiveBlock();

        [TearDown]
        public void TearDown() => ExclusiveBlock?.Dispose();

        [Test]
        public void Acquire_ShouldThrowOnParallelInvocation()
        {
            for (int i = 0; i < 5; i++)
            {
                using (ExclusiveBlock.Enter())
                {
                    Assert.ThrowsAsync<InvalidOperationException>(() => Task.Run(ExclusiveBlock.Enter), Resources.NOT_EXCLUSIVE);
                }
            }
        }

        [Test]
        public void Acquire_ShouldNotThrowOnSubsequentInvocation()
        {
            for (int i = 0; i < 5; i++)
            {
                using (ExclusiveBlock.Enter())
                {
                    using (ExclusiveBlock.Enter())
                    {
                        using (ExclusiveBlock.Enter())
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
                using (ExclusiveBlock.Enter())
                {
                    // calls MethodB()
                    using (ExclusiveBlock.Enter())
                    {
                        // calls MethodC()
                        using (ExclusiveBlock.Enter())
                        {
                            // MethodC() is called parallelly
                            Assert.ThrowsAsync<InvalidOperationException>(() => Task.Run(ExclusiveBlock.Enter), Resources.NOT_EXCLUSIVE);
                        }
                    }
                }
            }
        }
    }
}
