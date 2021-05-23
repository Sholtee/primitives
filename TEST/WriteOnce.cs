/********************************************************************************
* WriteOnce.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.Primitives.Patterns.Tests
{
    using Properties;

    [TestFixture]
    public class WriteOnceTests
    {
        [Test]
        public void GetValue_ShouldThrowIfTheValueIsNotSet() 
        {
            var wo = new WriteOnce();

            object val;
            Assert.Throws<InvalidOperationException>(() => val = wo.Value, Resources.NO_VALUE);

            wo.Value = new object();
            Assert.DoesNotThrow(() => val = wo.Value);
        }

        [Test]
        public void SetValue_ShouldThrowIfTheValueIsAlreadySet() 
        {
            var wo = new WriteOnce();

            Assert.DoesNotThrow(() => wo.Value = new object());
            Assert.Throws<InvalidOperationException>(() => wo.Value = new object(), Resources.VALUE_ALREADY_SET);
        }

        [Test]
        public void Value_MayBeNull() 
        {
            var wo = new WriteOnce(strict: true);
            Assert.DoesNotThrow(() => wo.Value = null);
            Assert.That(wo.Value, Is.Null);
        }
    }
}
