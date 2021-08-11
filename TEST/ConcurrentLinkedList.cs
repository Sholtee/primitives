/********************************************************************************
* ConcurrentLinkedList.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Solti.Utils.Primitives.Threading.Tests
{
    [TestFixture]
    public class ConcurrentLinkedListTests
    {
        public ConcurrentLinkedList List { get; set; }

        [SetUp]
        public void Setup()
        {
            List = new ConcurrentLinkedList();
        }

        [Test]
        public void ThreadingTest_UsingParallelAddAndRemove()
        {
            Assert.DoesNotThrowAsync(() => Task.WhenAll(Enumerable.Repeat(0, 100).Select((_, i) => Task.Run(() => 
            {
                Thread.Sleep(new Random().Next(0, 2));

                IntNode[] nodes = Enumerable.Repeat(0, 1000).Select(_ =>
                {
                    var res = new IntNode { Value = i };
                    List.Add(res);
                    return res;
                }).ToArray();

                foreach (IntNode node in nodes)
                {
                    Assert.DoesNotThrow(node.Dispose);
                    Assert.That(node.Owner, Is.Null);
                    Assert.That(node.Prev, Is.Null);
                    Assert.That(node.Next, Is.Null);
                }
            }))));

            Assert.That(List.Count, Is.EqualTo(0));
        }

        [Test]
        public void ThreadingTest_UsingParallelAddAndForEach()
        {
            Assert.DoesNotThrowAsync(() => Task.WhenAll(Enumerable.Repeat(0, 100).Select((_, i) => Task.Run(() =>
            {
                Thread.Sleep(new Random().Next(0, 2));

                for (int j = 0; j < 1000; j++)
                {
                    List.Add(new IntNode { Value = i });
                }

                Assert.That(List.Cast<IntNode>().Count(node => node.Value == i), Is.EqualTo(1000));
            }))));
        }

        [Test]
        public void ThreadingTest_UsingParallelAddRemoveAndForEach()
        {
            Assert.DoesNotThrowAsync(() => Task.WhenAll(Enumerable.Repeat(0, 100).Select((_, i) => Task.Run(() =>
            {
                Thread.Sleep(new Random().Next(0, 2));

                for (int j = 0; j < 1000; j++)
                {
                    List.Add(new IntNode { Value = i });
                }

                IntNode[] toBeRemoved = List
                    .Cast<IntNode>()
                    .Where(node => node.Value == i)
                    .ToArray();

                Assert.That(toBeRemoved.Length, Is.EqualTo(1000));

                foreach (IntNode node in toBeRemoved)
                {
                    Assert.DoesNotThrow(node.Dispose);
                    Assert.That(node.Owner, Is.Null);
                    Assert.That(node.Prev, Is.Null);
                    Assert.That(node.Next, Is.Null);
                }                
            }))));

            Assert.That(List.Count, Is.EqualTo(0));
        }

        [Test]
        public void EmptyList_CanBeEnumerated()
        {
            Assert.DoesNotThrowAsync(() => Task.Run(() => List.Count()));
            Assert.DoesNotThrow(() => List.Add(new IntNode()));
        }

        [Test]
        public void Remove_ShouldThrowInsideAForeachLoop()
        {
            LinkedListNode node = new();
            List.Add(node);

            foreach (LinkedListNode x in List)
                Assert.Throws<InvalidOperationException>(() => List.Remove(x));

            Assert.DoesNotThrow(node.Dispose);
        }

        private class IntNode : LinkedListNode
        {
            public int Value { get; set; }
        }
    }
}
