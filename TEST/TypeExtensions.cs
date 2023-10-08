/********************************************************************************
* TypeExtensions.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace Solti.Utils.Primitives.Tests
{
    [TestFixture]
    public class TypeExtensionsTests
    {
        [TestCase(typeof(object), "System.Object")]
        [TestCase(typeof(IList<>), "System.Collections.Generic.IList{T}")]
        [TestCase(typeof(IList<object>), "System.Collections.Generic.IList{System.Object}")]
        [TestCase(typeof(IDictionary<string, TypeExtensionsTests>), "System.Collections.Generic.IDictionary{System.String, Solti.Utils.Primitives.Tests.TypeExtensionsTests}")]
        public void GetFriendlyName_ShouldDoWhatTheNameSuggests(Type type, string name) => Assert.That(type.GetFriendlyName(), Is.EqualTo(name));
    }
}
