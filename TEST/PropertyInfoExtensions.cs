/********************************************************************************
* PropertyInfoExtensions.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

using NUnit.Framework;

namespace Solti.Utils.Primitives.Tests
{
    [TestFixture]
    public class PropertyInfoExtensions
    {
        [Test]
        public void Getter_ShouldGetTheDesiredProperty() 
        {
            var lst = new List<int>();
            lst.Add(1986);

            Assert.That(typeof(List<int>).GetProperty(nameof(List<int>.Count)).ToGetter().Invoke(lst), Is.EqualTo(1));
        }

        [Test]
        public void Setter_ShouldSetTheDesiredProperty()
        {
            var lst = new List<int>();
            typeof(List<int>).GetProperty(nameof(List<int>.Capacity)).ToSetter().Invoke(lst, 1);

            Assert.That(lst.Capacity, Is.EqualTo(1));
        }
    }
}
