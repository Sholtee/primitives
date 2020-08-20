/********************************************************************************
* Singleton.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.Primitives.Patterns.Tests
{
    using Properties;

    [TestFixture]
    public class SingletonTests
    {
        private class MySingleton : Singleton<MySingleton> 
        { 
        }

        [Test]
        public void Ctor_ShouldThrowIfThereIsAnExistingInstance() 
        {
            Assert.That(MySingleton.Instance, Is.Null);

            MySingleton inst = null;
            Assert.DoesNotThrow(() => inst = new MySingleton());
            Assert.AreSame(inst, MySingleton.Instance);
            Assert.Throws<InvalidOperationException>(() => new MySingleton(), Resources.INSTANCE_ALREADY_CREATED);
        }
    }
}
