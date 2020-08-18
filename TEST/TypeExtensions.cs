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
        [TestCase(typeof(object), "object")]
        [TestCase(typeof(IList<object>), "System.Collections.Generic.IList{object}")]
        public void GetFriendlyName_ShouldDoWhatTheNameSuggests(Type type, string name) => Assert.That(type.GetFriendlyName(), Is.EqualTo(name));
    }
}
