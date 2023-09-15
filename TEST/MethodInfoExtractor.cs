/********************************************************************************
* MethodInfoExtractor.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Reflection;

using NUnit.Framework;

namespace Solti.Utils.Primitives.Tests
{
    [TestFixture]
    public class MethodInfoExtractor
    {
        [Test]
        public void Extract_ShouldExtractFromInstanceExpression()
        {
            MethodInfo meth = Primitives.MethodInfoExtractor.Extract<IList>(l => l.IndexOf(null!));
            Assert.That(meth, Is.EqualTo(typeof(IList).GetMethod("IndexOf")));
        }

        [Test]
        public void Extract_ShouldExtractFromStaticExpression()
        {
            MethodInfo meth = Primitives.MethodInfoExtractor.Extract(() => Object.Equals(null, null));
            Assert.That(meth, Is.EqualTo(typeof(Object).GetMethod("Equals", BindingFlags.Public | BindingFlags.Static)));
        }
    }
}
