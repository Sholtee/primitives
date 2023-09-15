/********************************************************************************
* PropertyInfoExtractor.cs                                                      *
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
    public class PropertyInfoExtractor
    {
        [Test]
        public void Extract_ShouldExtractFromInstanceExpression()
        {
            PropertyInfo prop = Primitives.PropertyInfoExtractor.Extract<IList, int>(l => l.Count);
            Assert.That(prop, Is.EqualTo(typeof(ICollection).GetProperty("Count")));
        }

        [Test]
        public void Extract_ShouldExtractFromStaticExpression()
        {
            PropertyInfo prop = Primitives.PropertyInfoExtractor.Extract(() => AppDomain.CurrentDomain);
            Assert.That(prop, Is.EqualTo(typeof(AppDomain).GetProperty("CurrentDomain")));
        }
    }
}
