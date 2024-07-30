/********************************************************************************
* MemoryExtensionsTests.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace Solti.Utils.Primitives.Tests
{
    [TestFixture]
    public class MemoryExtensionsTests
    {
        [TestCase("a")]
        [TestCase("1a")]
        [TestCase("abcd")]
        [TestCase("aábcdeé")]
        [TestCase("12345abcd")]
        public void GetHashCode_ShouldHash(string val)
        {
            Assert.DoesNotThrow(() => val.AsSpan().GetHashCode(false));
            Assert.AreEqual(val.AsSpan().GetHashCode(false), val.AsSpan().GetHashCode(false));
            Assert.AreNotEqual(val.AsSpan().GetHashCode(false), val.ToUpper().AsSpan().GetHashCode(false));
        }

        [TestCase("a")]
        [TestCase("1a")]
        [TestCase("abcd")]
        [TestCase("ABCD")]
        [TestCase("aábcdeé")]
        [TestCase("AÁBCDEÉ")]
        public void GetHashCode_ShouldHashIgnoringCasing(string val)
        {
            Assert.DoesNotThrow(() => val.AsSpan().GetHashCode(true));
            Assert.AreEqual(val.AsSpan().GetHashCode(true), val.AsSpan().GetHashCode(true));
            Assert.AreEqual(val.ToLower().AsSpan().GetHashCode(true), val.ToUpper().AsSpan().GetHashCode(true));
        }

        [Test]
        public void GetHashCode_ShouldNotCollide()
        {
            const int iterations = short.MaxValue * 100;

            HashSet<int> hashes = [];

            for (int i = 0; i < iterations; i++)
            {
                hashes.Add(i.ToString().AsSpan().GetHashCode(false));
            }

            long collissions = iterations - hashes.Count;
            Assert.That(collissions / (double) iterations, Is.LessThan(0.0004));
        }

        const string TEST_STR = "0123456789+-.eE";

        [Test]
        public void IndexOfAnyExcept_ShouldReturnMinusOneIfThereIsNoMatch() =>
            Assert.That(TEST_STR.AsSpan().IndexOfAnyExcept(searchValues: TEST_STR.AsSpan()), Is.EqualTo(-1));

        [Test]
        public void IndexOfAnyExcept_ShouldReturnTheAppropriateIndex([Values("1\t", "\t", "1a", "a", "1á", "á")] string input) =>
            Assert.That(input.AsSpan().IndexOfAnyExcept(searchValues: TEST_STR.AsSpan()), Is.EqualTo(input.Length - 1));
    }
}
