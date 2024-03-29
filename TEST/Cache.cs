﻿/********************************************************************************
* Cache.cs                                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using NUnit.Framework;

namespace Solti.Utils.Primitives.Tests
{
    [TestFixture]
    public sealed class CacheTests
    {
        const string key = "key";

        [Test]
        public void Cache_ShouldBeScoped()
        {
            Assert.AreSame(CacheUsage_1(), CacheUsage_1());
            Assert.AreSame(CacheUsage_2(), CacheUsage_2());
            Assert.AreNotSame(CacheUsage_1(), CacheUsage_2());
        }

        static object CacheUsage_1() => Cache.GetOrAdd(key, _ => new object());
        static object CacheUsage_2() => Cache.GetOrAdd(key, _ => new object());

        [Test]
        public void Cache_ShouldHandleComplexKeys() 
        {
            Assert.AreSame(Cache.GetOrAdd(new {k1 = typeof(object), k2 = "cica"}, _ => new object()), Cache.GetOrAdd(new { k1 = typeof(object), k2 = "cica" }, _ => new object()));
            Assert.AreSame(Cache.GetOrAdd((typeof(object), "cica"), _ => new object()), Cache.GetOrAdd((typeof(object), "cica"), _ => new object()));
        }

        [Test]
        public void CacheSlim_ShouldHandleComplexKeys()
        {
            Assert.AreSame(CacheSlim.GetOrAdd(new { k1 = typeof(object), k2 = "cica" }, _ => new object()), CacheSlim.GetOrAdd(new { k1 = typeof(object), k2 = "cica" }, _ => new object()));
            Assert.AreSame(CacheSlim.GetOrAdd((typeof(object), "cica"), _ => new object()), CacheSlim.GetOrAdd((typeof(object), "cica"), _ => new object()));
        }
    }
}
