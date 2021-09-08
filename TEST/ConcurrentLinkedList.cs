/********************************************************************************
* ConcurrentLinkedList.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Solti.Utils.Primitives.Threading.Tests
{
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
                LinkedListNode<int>[] nodes = Enumerable
                    .Repeat(0, 3000)
                    .Select(_ => List.Add(i))
                    .ToArray();

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
        public void ThreadingTest_UsingParallelAddAndTakeFirst()
        {
            Assert.DoesNotThrowAsync(() => Task.WhenAll(Enumerable.Repeat(0, 100).Select((_, _) => Task.Run(() =>
            {
                for (int i = 0; i < 3000; i++)
                {
                    List.Add(0);
                }

                for (int i = 0; i < 3000; i++)
                {
                    Assert.That(List.TakeFirst(out _));
                }
            }))));

            Assert.That(List, Is.Empty);
        }

        [Test]
        public void ThreadingTest_UsingParallelAddAndForEach()
        {
            Assert.DoesNotThrowAsync(() => Task.WhenAll(Enumerable.Repeat(0, 100).Select((_, i) => Task.Run(() =>
            {
                for (int j = 0; j < 3000; j++)
                {
                    List.Add(i);
                }

                Assert.That(List.Count(x => x == i), Is.EqualTo(3000));
            }))));
        }

        [Test]
        public void ThreadingTest_UsingParallelAddRemoveAndForEach()
        {
            Assert.DoesNotThrowAsync(() => Task.WhenAll(Enumerable.Repeat(0, 100).Select((_, i) => Task.Run(() =>
            {
                LinkedListNode<int>[] toBeRemoved = Enumerable
                    .Repeat(0, 3000)
                    .Select((_, i) => List.Add(i))
                    .ToArray();

                foreach (LinkedListNode<int> node in toBeRemoved)
                {
                    Assert.DoesNotThrow(() => List.Remove(node));
                    Assert.That(node.Owner, Is.Null);
                    Assert.That(node.Prev, Is.Null);
                    Assert.That(node.Next, Is.Null);
                }                
            }))));

            Assert.That(List, Is.Empty);
        }

        [Test]
        public void EmptyList_CanBeEnumerated()
        {
            Assert.DoesNotThrowAsync(() => Task.Run(() => List.Count()));
            Assert.That(List.Head.LockedBy, Is.EqualTo(0));
            Assert.DoesNotThrow(() => List.Add(0));
        }

        [Test]
        public void Enumeration_MayBeBroken()
        {
            LinkedListNode<int>
                node1 = List.Add(0),
                node2 = List.Add(1);

            foreach (int x in List)
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
            LinkedListNode<int> node = List.Add(0);

            foreach (int x in List)
            {
                Assert.Throws<InvalidOperationException>(() => List.Remove(node));
            }

            Assert.DoesNotThrow(() => List.Remove(node));
        }
    }
}
