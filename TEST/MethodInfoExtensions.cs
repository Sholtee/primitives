/********************************************************************************
* MethodInfoExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Reflection;

using NUnit.Framework;

namespace Solti.Utils.Primitives.Tests
{
    [TestFixture]
    public sealed class MethodInfoExtensions
    {
        private static bool Static() => true;
        private static bool ParameterizedStatic(bool b) => b;
        private static void VoidStatic() { }
        private bool Instance() => true;
        private bool ParameterizedInstance(bool b) => b;
        private void VoidInstance() { }

        [Test]
        public void ToStaticDelegate_ShouldHandleParameters() =>
            Assert.That(GetType().GetMethod(nameof(ParameterizedStatic), BindingFlags.Static | BindingFlags.NonPublic).ToStaticDelegate().Invoke(new object[] { true }), Is.True);

        [Test]
        public void ToStaticDelegate_ShouldHandleNoParameters() =>
            Assert.That(GetType().GetMethod(nameof(Static), BindingFlags.Static | BindingFlags.NonPublic).ToStaticDelegate().Invoke(new object[0] { }), Is.True);

        [Test]
        public void ToStaticDelegate_ShouldHandleVoidReturn() => 
            Assert.That(GetType().GetMethod(nameof(VoidStatic), BindingFlags.Static | BindingFlags.NonPublic).ToStaticDelegate().Invoke(new object[0] { }), Is.Null);

        [Test]
        public void ToStaticDelegate_ShouldInstantiate() => 
            Assert.That(typeof(List<int>).GetConstructor(Type.EmptyTypes).ToStaticDelegate().Invoke(new object[0] { }), Is.InstanceOf<List<int>>());

        [Test]
        public void ToInstanceDelegate_ShouldHandleParameters() =>
            Assert.That(GetType().GetMethod(nameof(ParameterizedInstance), BindingFlags.Instance | BindingFlags.NonPublic).ToInstanceDelegate().Invoke(this, new object[] { true }), Is.True);

        [Test]
        public void ToInstanceDelegate_ShouldHandleNoParameters() =>
            Assert.That(GetType().GetMethod(nameof(Instance), BindingFlags.Instance | BindingFlags.NonPublic).ToInstanceDelegate().Invoke(this, new object[0] { }), Is.True);

        [Test]
        public void ToInstanceDelegate_ShouldHandleVoidReturn() =>
            Assert.That(GetType().GetMethod(nameof(VoidInstance), BindingFlags.Instance | BindingFlags.NonPublic).ToInstanceDelegate().Invoke(this, new object[0] { }), Is.Null);
    }
}
