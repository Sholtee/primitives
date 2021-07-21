/********************************************************************************
* Composite.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.Primitives.Patterns.Tests
{
    using Properties;

    [TestFixture]
    public sealed class CompositeTests
    {
        private interface IMyComposite : IComposite<IMyComposite>
        {
        }

        private class MyComposite : Composite<IMyComposite>, IMyComposite
        {
        }

        [Test]
        public void Dispose_ShouldFreeTheChildrenRecursively() 
        {
            IMyComposite 
                root = new MyComposite(),
                child = new MyComposite { Parent = root },
                grandChild = new MyComposite { Parent = child };

            root.Dispose();

            Assert.Throws<ObjectDisposedException>(grandChild.Dispose);
            Assert.Throws<ObjectDisposedException>(child.Dispose);
        }

        [Test]
        public async Task DisposeAsync_ShouldFreeTheChildrenRecursively()
        {
            IMyComposite
                root = new MyComposite(),
                child = new MyComposite { Parent = root },
                grandChild = new MyComposite { Parent = child };

            await root.DisposeAsync();

            Assert.Throws<ObjectDisposedException>(grandChild.Dispose);
            Assert.Throws<ObjectDisposedException>(child.Dispose);
        }

        [Test]
        public void Dispose_ShouldRemoveTheChildFromTheParentsChildrenList() 
        {
            IMyComposite
                root = new MyComposite(),
                child = new MyComposite { Parent = root };

            new MyComposite { Parent = root }; // harmadik

            Assert.That(root.Children.Count, Is.EqualTo(2));
            child.Dispose();
            Assert.That(root.Children.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task DisposeAsync_ShouldRemoveTheChildFromTheParentsChildrenList()
        {
            IMyComposite
                root = new MyComposite(),
                child = new MyComposite { Parent = root };

            new MyComposite { Parent = root }; // harmadik

            Assert.That(root.Children.Count, Is.EqualTo(2));
            await child.DisposeAsync();
            Assert.That(root.Children.Count, Is.EqualTo(1));
        }

        [Test]
        public void AddChild_ShouldValidate() 
        {
            IMyComposite 
                root = new MyComposite(),
                child = new MyComposite();

            Assert.Throws<ArgumentNullException>(() => root.Children.Add(null));
            Assert.DoesNotThrow(() => child.Parent = root);
            Assert.Throws<InvalidOperationException>(() => root.Children.Add(child), Resources.ITEM_ALREADY_ADDED);
        }

        [Test]
        public void Children_ShouldBeThreadSafe()
        {
            IMyComposite root = new MyComposite();

            Assert.DoesNotThrowAsync(() => Task.WhenAll(Enumerable.Repeat(0, 50).Select(_ => Task.Run(() =>
            {
                MyComposite child = new() { Parent = root };
                Random rnd = new();

                Thread.Sleep(rnd.Next(0, 5));
                child.Parent = null;
            }))));

            Assert.That(root.Children, Is.Empty);
        }

        [Test]
        public void Children_MayBeLimited() 
        {
            IMyComposite root = new MyComposite { MaxChildCount = 1 };

            Assert.DoesNotThrow(() => new MyComposite { Parent = root });
            Assert.Throws<InvalidOperationException>(() => new MyComposite { Parent = root }, Resources.MAX_SIZE_REACHED);
        }

        [Test]
        public void RemoveChild_ShouldValidate()
        {
            IMyComposite
                root = new MyComposite(),
                child = new MyComposite { Parent = root };

            Assert.Throws<ArgumentNullException>(() => root.Children.Remove(null));
            Assert.That(() => root.Children.Remove(new MyComposite()), Is.False);

            //
            // Nem lett felszabaditva.
            //

            Assert.DoesNotThrow(child.Dispose);
            Assert.That(root.Children.Count, Is.EqualTo(0));
        }

        [Test]
        public void ContainsChild_ShouldDoWhatItsNameSuggests() 
        {
            IMyComposite
                root = new MyComposite(),
                child = new MyComposite();


            Assert.That(root.Children.Contains(child), Is.False);

            root.Children.Add(child);
            Assert.That(root.Children.Contains(child));
        }

        public interface IRealComposite : IComposite<IRealComposite> 
        {
            void Foo(int arg);
            string Bar();
        }

        private class RealComposite : Composite<IRealComposite>, IRealComposite
        {
            public void Foo(int arg) // direkt nem explicit
            {
                Dispatch(i => i.Foo(arg));
            }

            string IRealComposite.Bar()
            {
                return string.Join(" ", Dispatch(i => i.Bar()));
            }
        }

        [Test]
        public void Dispatch_ShouldInvokeTheChildMethodsWithTheAppropriateArguments() 
        {
            IRealComposite root = new RealComposite();

            Mock<IRealComposite> 
                child1 = CreateChild(),
                child2 = CreateChild();
            
            Mock<IRealComposite> CreateChild() 
            {
                var result = new Mock<IRealComposite>(MockBehavior.Strict);

                result
                    .Setup(i => i.Bar())
                    .Returns("cica");

                result
                    .Setup(i => i.Foo(1986));

                IComposite<IRealComposite> parent = null;

                result
                    .SetupGet(i => i.Parent)
                    .Returns(() => parent);

#pragma warning disable CS0618 // Type or member is obsolete
                result
                    .SetupSet(i => i.Parent)
#pragma warning restore CS0618
                    .Callback(val => parent = val);

                result
                    .Setup(i => i.Dispose())
                    .Callback(() => root.Children.Remove(result.Object));

                return result;
            }

            root.Children.Add(child1.Object);
            root.Children.Add(child2.Object);

            root.Foo(1986);

            child1.Verify(i => i.Foo(1986), Times.Once);
            child2.Verify(i => i.Foo(1986), Times.Once);

            Assert.That(root.Bar(), Is.EqualTo("cica cica"));

            root.Dispose();

            child1.Verify(i => i.Dispose(), Times.Once);
            child2.Verify(i => i.Dispose(), Times.Once);
        }

        [Test]
        public void Dispatch_ShouldTraverseOnHierarchyDownwards() 
        {
            IRealComposite 
                root = new RealComposite(),
                child = new RealComposite();

            var mockGrandChild = new Mock<IRealComposite>(MockBehavior.Strict);

            mockGrandChild
                .Setup(i => i.Bar())
                .Returns("cica");

            IComposite<IRealComposite> parent = null;

            mockGrandChild
                .SetupGet(i => i.Parent)
                .Returns(() => parent);

#pragma warning disable CS0618 // Type or member is obsolete
            mockGrandChild
                .SetupSet(i => i.Parent)
#pragma warning restore CS0618
                    .Callback(val => parent = val);

            child.Children.Add(mockGrandChild.Object);
            root.Children.Add(child);

            root.Bar();

            mockGrandChild.Verify(i => i.Bar(), Times.Once);
        }

        [Test]
        public void Dispatch_ShouldValidate() 
        {
            var root = new RealComposite();

            Assert.Throws<ArgumentNullException>(() => root.Dispatch(null));
        }

        private class BadComposite : Composite<IMyComposite> 
        {
        }

        [Test]
        public void Parent_ShouldThrowIfTheInterfaceIsNotImplemented() =>
            Assert.Throws<NotSupportedException>(() => new BadComposite { Parent = new BadComposite() }, Resources.INTERFACE_NOT_SUPPORTED);

        public interface IGeneric: IComposite<IGeneric>
        {
            void Foo<T>(T p);
        }

        private class GenericComposite : Composite<IGeneric>, IGeneric 
        {
            public void Foo<T>(T p) => Dispatch(i => i.Foo(p));
        }

        [Test]
        public void Dispatch_ShouldSupportGenericMethods() 
        {
            var mockChild = new Mock<IGeneric>(MockBehavior.Strict);

            IComposite<IGeneric> parent = null;

            mockChild
                .SetupGet(i => i.Parent)
                .Returns(() => parent);

#pragma warning disable CS0618 // Type or member is obsolete
            mockChild
                .SetupSet(i => i.Parent)
#pragma warning restore CS0618
                    .Callback(val => parent = val);

            mockChild.Setup(i => i.Foo(1));

            var root = new GenericComposite();
            root.Children.Add(mockChild.Object);

            root.Foo(1);

            mockChild.Verify(i => i.Foo(1), Times.Once);        
        }
    }
}
