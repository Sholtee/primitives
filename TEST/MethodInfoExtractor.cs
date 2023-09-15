/********************************************************************************
* MethodInfoExtractor.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
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
        public void Extract_ShouldExtractFromInstanceExpressionHavingOutParameter()
        {
            MethodInfo meth = Primitives.MethodInfoExtractor.Extract<Dictionary<string, object>, object>((dict, obj) => dict.TryGetValue(null!, out obj));
            Assert.That(meth, Is.EqualTo(typeof(Dictionary<string, object>).GetMethod("TryGetValue")));
        }

        [Test]
        public void Extract_ShouldExtractFromStaticExpression()
        {
            MethodInfo meth = Primitives.MethodInfoExtractor.Extract(() => Object.Equals(null, null));
            Assert.That(meth, Is.EqualTo(typeof(Object).GetMethod("Equals", BindingFlags.Public | BindingFlags.Static)));
        }
    }
}
