/********************************************************************************
* ConstructorInfoExtractor.cs                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Reflection;

using NUnit.Framework;

namespace Solti.Utils.Primitives.Tests
{
    [TestFixture]
    public class ConstructorInfoExtractor
    {
        [Test]
        public void Extract_ShouldExtractFromStaticExpression()
        {
            ConstructorInfo ctor = Primitives.ConstructorInfoExtractor.Extract(() => new Dictionary<object, object>(0));
            Assert.That(ctor, Is.EqualTo(typeof(Dictionary<object, object>).GetConstructor(new[] { typeof(int) })));
        }
    }
}
