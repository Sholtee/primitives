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
    using Properties;

    [TestFixture]
    public class ConcurrentLinkedListTests
    {
        public ConcurrentLinkedList<int> List { get; set; }

        [SetUp]
        public void Setup()
        {
            List = new ConcurrentLinkedList<int>();
        }

        [Test]
        public void ThreadingTest_UsingParallelAddAndRemove()
        {
            Assert.DoesNotThrowAsync(() => Task.WhenAll(Enumerable.Repeat(0, 100).Select((_, i) => Task.Run(() => 
            {
                Thread.Sleep(new Random().Next(0, 2));

                LinkedListNode<int>[] nodes = Enumerable.Repeat(0, 1000).Select(_ =>
                {
                    var res = new LinkedListNode<int> { Value = i };
                    List.Add(res);
                    return res;
                }).ToArray();

                foreach (LinkedListNode<int> node in nodes)
                {
                    Assert.DoesNotThrow(() => List.Remove(node));
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
                    List.Add(new LinkedListNode<int> { Value = i });
                }

                Assert.That(List.Cast<LinkedListNode<int>>().Count(node => node.Value == i), Is.EqualTo(1000));
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
                    List.Add(new LinkedListNode<int> { Value = i });
                }

                LinkedListNode<int>[] toBeRemoved = List
                    .Cast<LinkedListNode<int>>()
                    .Where(node => node.Value == i)
                    .ToArray();

                Assert.That(toBeRemoved.Length, Is.EqualTo(1000));

                foreach (LinkedListNode<int> node in toBeRemoved)
                {
                    Assert.DoesNotThrow(() => List.Remove(node));
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
            Assert.That(List.Head.LockedBy, Is.EqualTo(0));
            Assert.DoesNotThrow(() => List.Add(new LinkedListNode<int>()));
        }

        [Test]
        public void Enumeration_MayBeBroken()
        {
            LinkedListNode<int>
                node1 = new(),
                node2 = new();

            List.Add(node1);
            List.Add(node2);

            foreach (LinkedListNode<int> node in List)
            {
                break;
            }

            Assert.That(List.Head.LockedBy, Is.EqualTo(0));
            Assert.That(node1.LockedBy, Is.EqualTo(0));
            Assert.That(node2.LockedBy, Is.EqualTo(0));
        }

        [Test]
        public void Remove_ShouldThrowInsideAForeachLoop()
        {
            LinkedListNode<int> node = new();
            List.Add(node);

            foreach (LinkedListNode<int> x in List)
                Assert.Throws<InvalidOperationException>(() => List.Remove(x));

            Assert.DoesNotThrow(() => List.Remove(node));
        }

        [Test]
        public void Add_ShouldThrowOnAlreadyOwnedNode()
        {
            LinkedListNode<int> node = new();
            List.Add(node);

            Assert.Throws<ArgumentException>(() => List.Add(node), Resources.ALREADY_OWNED);
            Assert.Throws<ArgumentException>(() => new ConcurrentLinkedList<int>().Add(node), Resources.ALREADY_OWNED);
        }
    }
}
