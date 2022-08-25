/********************************************************************************
* RedBlackTree.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace Solti.Utils.Primitives.Tests
{
    using Patterns;

    [TestFixture]
    public sealed class RedBlackTreeTests
    {
        private sealed class IntComparer : Singleton<IntComparer>, IComparer<int>
        {
            public int Compare(int x, int y) => x.CompareTo(y);
        }

        public IEnumerable<int> Values
        {
            get
            {
                Random random = new(1986);

                return Enumerable
                    .Repeat(0, 10000)
                    .Select((_, i) => i)
                    .OrderBy(_ => random.Next());
            }
        }

        [Test]
        public void Tree_ShouldBeBalanced([Values(0, 1, 5, 10, 100, 10000)] int take)
        {
            List<int> values = new(Values.Take(take));

            RedBlackTree<int> tree = new(IntComparer.Instance);

            foreach (int value in values)
            {
                tree.Add(value);
            }

            Assert.That(tree.Count, Is.EqualTo(take));
            Assert.That(tree.Select(node => node.Data).SequenceEqual(values.OrderBy(v => v)));
        }

        [Test]
        public void NewTree_ShouldBeBalanced([Values(0, 1, 5, 10, 100, 10000)] int take)
        {
            List<int> values = new(Values.Take(take));

            RedBlackTree<int> tree = new(IntComparer.Instance);

            foreach (int value in values)
            {
                tree = tree.With(value);
            }

            Assert.That(tree.Count, Is.EqualTo(take));
            Assert.That(tree.Select(node => node.Data).SequenceEqual(values.OrderBy(v => v)));
        }

        [Test]
        public void With_ShouldThrowIfTheNodeAlreadyContained()
        {
            RedBlackTree<int> tree = new(IntComparer.Instance);
            tree = tree.With(1);
            Assert.Throws<InvalidOperationException>(() => tree.With(1));
        }
    }
}
