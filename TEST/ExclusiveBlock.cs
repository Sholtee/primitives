/********************************************************************************
* ExclusiveBlock.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Reflection;
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
                    InvalidOperationException ex = Assert.ThrowsAsync<InvalidOperationException>(() => Task.Run(Acquire_ShouldThrowOnParallelInvocation), Resources.NOT_EXCLUSIVE);
                    Assert.That(ex.Data["method"], Is.EqualTo(MethodBase.GetCurrentMethod()));
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
                using (ExclusiveBlock.Enter())
                {
                    using (ExclusiveBlock.Enter())
                    {
                        using (ExclusiveBlock.Enter())
                        {
                            InvalidOperationException ex = Assert.ThrowsAsync<InvalidOperationException>(() => Task.Run(Acquire_ShouldThrowOnSubsequentParallelInvocation), Resources.NOT_EXCLUSIVE);
                            Assert.That(ex.Data["method"], Is.EqualTo(MethodBase.GetCurrentMethod()));
                        }
                    }
                }
            }
        }
    }
}
