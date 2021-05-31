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
        public void Setup() => ExclusiveBlock = new ExclusiveBlock(ExclusiveBlockFeatures.SupportsRecursion);

        [TearDown]
        public void TearDown() => ExclusiveBlock?.Dispose();

        [Test]
        public void Enter_ShouldThrowOnParallelInvocation()
        {
            for (int i = 0; i < 5; i++)
            {
                using (ExclusiveBlock.Enter())
                {
                    InvalidOperationException ex = Assert.ThrowsAsync<InvalidOperationException>(() => Task.Run(Enter_ShouldThrowOnParallelInvocation), Resources.NOT_EXCLUSIVE);
                    Assert.That(ex.Data["method"], Is.EqualTo(MethodBase.GetCurrentMethod()));
                }
            }
        }

        [Test]
        public void Enter_ShouldNotThrowOnRecursiveInvocations()
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
        public void Enter_ShouldThrowOnParallelInvocation2()
        {
            for (int i = 0; i < 5; i++)
            {
                using (ExclusiveBlock.Enter())
                {
                    using (ExclusiveBlock.Enter())
                    {
                        using (ExclusiveBlock.Enter())
                        {
                            InvalidOperationException ex = Assert.ThrowsAsync<InvalidOperationException>(() => Task.Run(Enter_ShouldThrowOnParallelInvocation2), Resources.NOT_EXCLUSIVE);
                            Assert.That(ex.Data["method"], Is.EqualTo(MethodBase.GetCurrentMethod()));
                        }
                    }
                }
            }
        }
    }
}
