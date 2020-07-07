/********************************************************************************
* TaskExtensions.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Solti.Utils.Primitives.Tests
{  
    using Threading;

    [TestFixture]
    public class TaskExtensionsTests
    {
        [Test]
        public async Task Cast_ShouldCastTheGivenTaskToThePassedType() 
        {
            var task = Task.Factory.StartNew<object>(() => "cica");
            var stringTask = task.Cast(typeof(string));

            Assert.That(stringTask, Is.InstanceOf<Task<string>>());
            Assert.That(await (Task<string>) stringTask, Is.EqualTo("cica"));
        }

        [Test]
        public async Task Cast_ShouldCastTheGivenTask()
        {
            var task = Task.Factory.StartNew<object>(() => "cica");
            var stringTask = task.Cast<object, string>();

            Assert.That(await stringTask, Is.EqualTo("cica"));
        }

        [Test]
        public void Cast_ShouldThrowIfTheCastIsNotPossible()
        {
            var task = Task.Factory.StartNew(() => "cica");
            Assert.ThrowsAsync<InvalidCastException>(() => task.Cast<string, int>());
        }
    }
}
