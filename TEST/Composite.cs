/********************************************************************************
* Composite.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
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
            public MyComposite(IMyComposite parent) : base(parent)
            {
            }

            public MyComposite() : this(null)
            {
            }
        }

        [Test]
        public void Dispose_ShouldFreeTheChildrenRecursively() 
        {
            IMyComposite 
                root = new MyComposite(),
                child = new MyComposite(root),
                grandChild = new MyComposite(child);

            root.Dispose();

            Assert.Throws<ObjectDisposedException>(grandChild.Dispose);
            Assert.Throws<ObjectDisposedException>(child.Dispose);
        }

        [Test]
        public async Task DisposeAsync_ShouldFreeTheChildrenRecursively()
        {
            IMyComposite
                root = new MyComposite(),
                child = new MyComposite(root),
                grandChild = new MyComposite(child);

            await root.DisposeAsync();

            Assert.Throws<ObjectDisposedException>(grandChild.Dispose);
            Assert.Throws<ObjectDisposedException>(child.Dispose);
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
            IMyComposite 
                root = new MyComposite(),
                child = new MyComposite(root);

            Assert.Throws<ArgumentNullException>(() => root.Children.Add(null));
            Assert.Throws<ArgumentException>(() => root.Children.Add(child), Resources.BELONGING_ITEM);
        }

        [Test]
        public void RemoveChild_ShouldValidate()
        {
            IMyComposite
                root = new MyComposite(),
                child = new MyComposite(root);

            Assert.Throws<ArgumentNullException>(() => root.Children.Remove(null));
            Assert.That(() => root.Children.Remove(new MyComposite()), Is.False);

            //
            // Nem lett felszabaditva.
            //

            Assert.DoesNotThrow(child.Dispose);
            Assert.That(root.Children.Count, Is.EqualTo(0));
        }

        [Test]
        public void Clear_ShouldNotDisposeTheChildren()
        {
            IMyComposite
                root = new MyComposite(),
                child = new MyComposite(root);

            Assert.That(root.Children.Count, Is.EqualTo(1));
            root.Children.Clear();

            //
            // Nem lett felszabaditva.
            //

            Assert.That(child.Parent, Is.Null);
            Assert.DoesNotThrow(child.Dispose);
            Assert.That(root.Children.Count, Is.EqualTo(0));
        }

        [Test]
        public void Parent_ShouldNotBeSetDirectly() 
        {
            IMyComposite
                root = new MyComposite(),
                child = new MyComposite(root);

            Assert.That(child.Parent, Is.EqualTo(root));
            Assert.Throws<InvalidOperationException>(() => child.Parent = null, Resources.CANT_SET_PARENT);
            
            root.Children.Remove(child);
            Assert.That(child.Parent, Is.Null);
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
            public RealComposite() : base(null)
            {
            }

            public void Foo(int arg) // direkt nem explicit
            {
                Dispatch(null, arg);
            }

            string IRealComposite.Bar()
            {
                return string.Join(" ", Dispatch(null));
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

                IRealComposite parent = null;

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

            IRealComposite parent = null;

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

            Assert.Throws<ArgumentNullException>(() => root.Dispatch(null, args: null));
            Assert.Throws<InvalidOperationException>(() => root.Dispatch(null), Resources.DISPATCH_NOT_ALLOWED);
        }

        private class BadComposite : Composite<IMyComposite> 
        {
            public BadComposite() : base(null) { }
        }

        [Test]
        public void Ctor_ShouldThrowIfTheInterfaceIsNotImplemented() =>
            Assert.Throws<NotSupportedException>(() => new BadComposite(), Resources.INTERFACE_NOT_SUPPORTED);

        private interface IByRef: IComposite<IByRef>
        {
            void Foo(ref int b);
        }

        private class ByRefComposite : Composite<IByRef>, IByRef
        {
            public ByRefComposite() : base(null) { }

            public void Foo(ref int b) => Dispatch(null, b);
        }

        [Test]
        public void Dispatch_ShouldThrowOnByRefParameter() =>
            Assert.Throws<NotSupportedException>(() => 
            {
                int i = 0;
                new ByRefComposite().Foo(ref i);
            }, Resources.BYREF_PARAM_NOT_SUPPORTED);

        public interface IGeneric: IComposite<IGeneric>
        {
            void Foo<T>(T p);
        }

        private class GenericComposite : Composite<IGeneric>, IGeneric 
        {
            public GenericComposite() : base(null) { }

            public void Foo<T>(T p) => Dispatch(new[] { typeof(T) }, p);
        }

        [Test]
        public void Dispatch_ShouldSupportGenericMethods() 
        {
            var mockChild = new Mock<IGeneric>(MockBehavior.Strict);

            IGeneric parent = null;

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
