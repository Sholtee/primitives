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
        private interface IMyComposite : IComposite<IMyComposite>, INotifyOnDispose
        {
        }

        private class MyComposite : Composite<IMyComposite>, IMyComposite
        {
            public MyComposite(IMyComposite parent = null, int maxChildCount = int.MaxValue) : base(parent, maxChildCount) { }
        }

        [Test]
        public void Dispose_ShouldFreeTheChildrenRecursively() 
        {
            IMyComposite 
                root = new MyComposite(),
                child = new MyComposite(root),
                grandChild = new MyComposite(child);

            root.Dispose();

            Assert.That(grandChild.Disposed);
            Assert.That(child.Disposed);
        }

        [Test]
        public async Task DisposeAsync_ShouldFreeTheChildrenRecursively()
        {
            IMyComposite
                root = new MyComposite(),
                child = new MyComposite(root),
                grandChild = new MyComposite(child);

            await root.DisposeAsync();

            Assert.That(grandChild.Disposed);
            Assert.That(child.Disposed);
        }

        [Test]
        public void Dispose_ShouldRemoveTheChildFromTheParentsChildrenList() 
        {
            IMyComposite
                root = new MyComposite(),
                child = new MyComposite(root);

            new MyComposite(root); // harmadik

            Assert.That(root.Children.Count, Is.EqualTo(2));
            child.Dispose();
            Assert.That(root.Children.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task DisposeAsync_ShouldRemoveTheChildFromTheParentsChildrenList()
        {
            IMyComposite
                root = new MyComposite(),
                child = new MyComposite(root);

            new MyComposite(root); // harmadik

            Assert.That(root.Children.Count, Is.EqualTo(2));
            await child.DisposeAsync();
            Assert.That(root.Children.Count, Is.EqualTo(1));
        }

        [Test]
        public void AddChild_ShouldValidate() 
        {
            IMyComposite root = new MyComposite();

            Assert.Throws<ArgumentNullException>(() => root.Children.Add(null));
        }

        [Test]
        public void Children_ShouldBeThreadSafe()
        {
            IMyComposite root = new MyComposite();

            Assert.DoesNotThrowAsync(() => Task.WhenAll(Enumerable.Repeat(0, 50).Select(_ => Task.Run(() =>
            {
                MyComposite child = new(root);
                Random rnd = new();

                Thread.Sleep(rnd.Next(0, 2));
                child.Dispose();
            }))));

            Assert.That(root.Children, Is.Empty);
        }

        [Test]
        public void Children_MayBeLimited() 
        {
            IMyComposite root = new MyComposite(maxChildCount: 1);

            Assert.DoesNotThrow(() => new MyComposite(root));
            Assert.Throws<InvalidOperationException>(() => new MyComposite(root), Resources.MAX_SIZE_REACHED);
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

        public interface IRealComposite : IComposite<IRealComposite>, INotifyOnDispose
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

                result
                    .Setup(i => i.Dispose())
                    .Raises(i => i.OnDispose += null, this, EventArgs.Empty);

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
            public BadComposite(BadComposite parent = null) : base(parent) { }
        }

        [Test]
        public void Parent_ShouldThrowIfTheInterfaceIsNotImplemented() =>
            Assert.Throws<NotSupportedException>(() => new BadComposite(new BadComposite()), Resources.INTERFACE_NOT_SUPPORTED);

        public interface IGeneric: IComposite<IGeneric>, INotifyOnDispose
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

            mockChild.Setup(i => i.Foo(1));

            var root = new GenericComposite();
            root.Children.Add(mockChild.Object);

            root.Foo(1);

            mockChild.Verify(i => i.Foo(1), Times.Once);        
        }
    }
}
