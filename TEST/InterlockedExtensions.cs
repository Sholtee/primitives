/********************************************************************************
* InterlockedExtensions.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Solti.Utils.Primitives.Tests
{
    [TestFixture]
    public class InterlockedExtensionsTests
    {
        [Test]
        public void IncrementIfGreaterThan_ShouldWorkParallelly()
        {
            int value = 0;

            Task[] tasks = Enumerable
                .Repeat(0, 5)
                .Select(_ => Task.Run(Increment))
                .ToArray();

            Assert.DoesNotThrowAsync(() => Task.WhenAll(tasks));

            Assert.That(value, Is.EqualTo(5000));

            void Increment()
            {
                for (int i = 0; i < 1000; i++)
                {
                    InterlockedExtensions.IncrementIfGreaterThan(ref value, -1);
                }
            }
        }

        [Test]
        public void IncrementIfGreaterThan_ShouldDoNothingIfTheConditionFailes()
        {
            int value = 0;

            Assert.That(InterlockedExtensions.IncrementIfGreaterThan(ref value, 0) is null);
            Assert.That(value, Is.EqualTo(0));
        }

        [Test]
        public void IncrementIfLessThan_ShouldWorkParallelly()
        {
            int value = 0;

            Task[] tasks = Enumerable
                .Repeat(0, 5)
                .Select(_ => Task.Run(Increment))
                .ToArray();

            Assert.DoesNotThrowAsync(() => Task.WhenAll(tasks));

            Assert.That(value, Is.EqualTo(5000));

            void Increment()
            {
                for (int i = 0; i < 1000; i++)
                {
                    InterlockedExtensions.IncrementIfLessThan(ref value, 5001);
                }
            }
        }

        [Test]
        public void IncrementIfLessThan_ShouldDoNothingIfTheConditionFailes()
        {
            int value = 0;

            Assert.That(InterlockedExtensions.IncrementIfLessThan(ref value, 0) is null);
            Assert.That(value, Is.EqualTo(0));
        }

        [Test]
        public void DecrementIfGreaterThan_ShouldWorkParallelly()
        {
            int value = 5000;

            Task[] tasks = Enumerable
                .Repeat(0, 5)
                .Select(_ => Task.Run(Decrement))
                .ToArray();

            Assert.DoesNotThrowAsync(() => Task.WhenAll(tasks));

            Assert.That(value, Is.EqualTo(0));

            void Decrement()
            {
                for (int i = 0; i < 1000; i++)
                {
                    InterlockedExtensions.DecrementIfGreaterThan(ref value, -1);
                }
            }
        }

        [Test]
        public void DecrementIfGreaterThan_ShouldDoNothingIfTheConditionFailes()
        {
            int value = 0;

            Assert.That(InterlockedExtensions.DecrementIfGreaterThan(ref value, 0) is null);
            Assert.That(value, Is.EqualTo(0));
        }

        [Test]
        public void Or_ShouldWorkParallelly()
        {
            int value = 0;

            Task[] tasks = Enumerable
                .Repeat(0, 5)
                .Select(_ => Task.Run(Or))
                .ToArray();

            Assert.DoesNotThrowAsync(() => Task.WhenAll(tasks));
            Assert.That((double) value, Is.EqualTo(Math.Pow(2, 31) - 1));

            void Or()
            {
                for (int i = 0; i < 31; i++)
                {
                    InterlockedExtensions.Or(ref value, 1 << i);
                }
            }
        }
    }
}
