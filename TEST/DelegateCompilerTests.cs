/********************************************************************************
* DelegateCompilerTests.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.Primitives.Tests
{
    using Properties;

    [TestFixture]
    public sealed class DelegateCompilerTests
    {
        [Test]
        public void Compile_ShouldSupportMultipleDelegates()
        {
            DelegateCompiler compiler = new();

            FutureDelegate<Func<int, string>> del1 = compiler.Register<Func<int, string>>(i => i.ToString());
            Assert.That(del1.IsCompiled, Is.False);
            Assert.Throws<InvalidOperationException>(() => _ = del1.Value, Resources.NOT_COMPILED);

            FutureDelegate<Func<string, int>> del2 = compiler.Register<Func<string, int>>(s => int.Parse(s));
            Assert.That(del2.IsCompiled, Is.False);
            Assert.Throws<InvalidOperationException>(() => _ = del2.Value, Resources.NOT_COMPILED);

            compiler.Compile();

            Assert.That(del1.IsCompiled);
            Assert.That(del2.IsCompiled);
            Assert.That(del1.Value(1986), Is.EqualTo("1986"));
            Assert.That(del2.Value("1986"), Is.EqualTo(1986));
        }

        [Test]
        public void Compile_ShouldNotThrowOnEmptyBatch()
        {
            DelegateCompiler compiler = new();
            Assert.DoesNotThrow(compiler.Compile);
        }
    }
}
