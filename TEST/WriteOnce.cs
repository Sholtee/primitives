/********************************************************************************
* WriteOnce.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.Primitives.Tests
{
    [TestFixture]
    public class WriteOnceTests
    {
        [Test]
        public void GetValue_ShouldThrowIfTheValueIsNotSet() 
        {
            var wo = new WriteOnce();

            object val;
            Assert.Throws<InvalidOperationException>(() => val = wo.Value);

            wo.Value = new object();
            Assert.DoesNotThrow(() => val = wo.Value);
        }

        [Test]
        public void SetValue_ShouldThrowIfTheValueIsAlreadySet() 
        {
            var wo = new WriteOnce();

            Assert.DoesNotThrow(() => wo.Value = new object());
            Assert.Throws<InvalidOperationException>(() => wo.Value = new object());
        }
    }
}
