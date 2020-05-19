/********************************************************************************
* DisposeByRefObject.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Solti.Utils.Primitives.Patterns.Tests
{
    using Properties;

    [TestFixture]
    public sealed class DisposeByRefObjectTests
    {
        [Test]
        public void AddRef_ShouldIncrementTheReferenceCount() 
        {
            var obj = new DisposeByRefObject();
            Assert.That(obj.RefCount, Is.EqualTo(1));
            Assert.That(obj.AddRef(), Is.EqualTo(2));
            Assert.That(obj.RefCount, Is.EqualTo(2));
        }

        [Test]
        public void AddRef_ShouldThrowIfTheObjectWasDisposed()
        {
            var obj = new DisposeByRefObject();
            Assert.That(obj.Release(), Is.EqualTo(0));
            Assert.Throws<ObjectDisposedException>(() => obj.AddRef());
        }

        [Test]
        public void Release_ShouldDecrementTheReferenceCount()
        {
            var obj = new DisposeByRefObject();
            Assert.That(obj.RefCount, Is.EqualTo(1));
            Assert.That(obj.Release(), Is.EqualTo(0));
            Assert.That(obj.RefCount, Is.EqualTo(0));
        }

        [Test]
        public void Release_ShouldDisposeTheObjectIfRefCountReachesTheZero() 
        {
            var obj = new DisposeByRefObject();
            obj.AddRef();
            Assert.That(obj.Release(), Is.EqualTo(1));
            Assert.That(obj.Disposed, Is.False);
            Assert.That(obj.Release(), Is.EqualTo(0));
            Assert.That(obj.Disposed);
        }

        [Test]
        public void Release_ShouldThrowIfTheObjectWasDisposed()
        {
            var obj = new DisposeByRefObject();
            Assert.That(obj.Release(), Is.EqualTo(0));
            Assert.Throws<ObjectDisposedException>(() => obj.Release());
        }

        [Test]
        public async Task ReleaseAsync_ShouldDisposeTheObjectIfRefCountReachesTheZero()
        {
            var obj = new DisposeByRefObject();
            obj.AddRef();
            Assert.That(obj.ReleaseAsync().Result, Is.EqualTo(1));
            Assert.That(obj.Disposed, Is.False);
            Assert.That(await obj.ReleaseAsync(), Is.EqualTo(0));
            Assert.That(obj.Disposed);
        }

        [Test]
        public void ReleaseAsync_ShouldThrowIfTheObjectWasDisposed() 
        {
            var obj = new DisposeByRefObject();
            Assert.That(obj.Release(), Is.EqualTo(0));
            Assert.ThrowsAsync<ObjectDisposedException>(async () => await obj.ReleaseAsync());
        }

        [Test]
        public void Dispose_ShouldNotBeCalledDirectly() 
        {
            var obj = new DisposeByRefObject();
            Assert.Throws<InvalidOperationException>(obj.Dispose, Resources.ARBITRARY_RELEASE);
        }

        [Test]
        public void DisposeAsync_ShouldNotBeCalledDirectly()
        {
            var obj = new DisposeByRefObject();
            Assert.ThrowsAsync<InvalidOperationException>(async () => await obj.DisposeAsync(), Resources.ARBITRARY_RELEASE);
        }
    }
}
