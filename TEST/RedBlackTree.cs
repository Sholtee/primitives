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
        private sealed class IntNode : RedBlackTreeNode
        {
            public int Value { get; }

            public IntNode(int value) : this(value, NodeColor.Unspecified)
            {
            }

            public IntNode(int value, NodeColor color) : base(color)
            {
                Value = value;
            }

            public override RedBlackTreeNode ShallowClone() => new IntNode(Value, Color);
        }

        private sealed class IntNodeComparer : Singleton<IntNodeComparer>, IComparer<IntNode>
        {
            public int Compare(IntNode x, IntNode y) => x.Value.CompareTo(y.Value);
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

            RedBlackTree<IntNode> tree = new(IntNodeComparer.Instance);

            foreach (int value in values)
            {
                tree.Add(new IntNode(value));
            }

            Assert.That(tree.Count, Is.EqualTo(take));
            Assert.That(tree.Select(node => node.Value).SequenceEqual(values.OrderBy(v => v)));
        }

        [Test]
        public void NewTree_ShouldBeBalanced([Values(0, 1, 5, 10, 100, 10000)] int take)
        {
            List<int> values = new(Values.Take(take));

            RedBlackTree<IntNode> tree = new(IntNodeComparer.Instance);

            foreach (int value in values)
            {
                tree = tree.With(new IntNode(value));
            }

            Assert.That(tree.Count, Is.EqualTo(take));
            Assert.That(tree.Select(node => node.Value).SequenceEqual(values.OrderBy(v => v)));
        }

        [Test]
        public void With_ShouldThrowIfTheNodeAlreadyContained()
        {
            RedBlackTree<IntNode> tree = new(IntNodeComparer.Instance);
            tree = tree.With(new IntNode(1));
            Assert.Throws<InvalidOperationException>(() => tree.With(new IntNode(1)));
        }
    }
}
