/********************************************************************************
* TypeExtensions.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;

using System.Text.RegularExpressions;

namespace Solti.Utils.Primitives
{
    /// <summary>
    /// Defines extensions for the <see cref="Type"/> type.
    /// </summary>
    public static class TypeExtensions
    {
        private static readonly Regex Replacer = new("[<>]", RegexOptions.Compiled);

        /// <summary>
        /// Gets the friendly name of the given type.
        /// </summary>
        public static string GetFriendlyName(this Type src)
        {
            if (src is null)
                throw new ArgumentNullException(nameof(src));

            return Cache.GetOrAdd(src, static src =>
            {
                using CodeDomProvider codeDomProvider = CodeDomProvider.CreateProvider("C#");
                CodeTypeReferenceExpression typeReferenceExpression = new(new CodeTypeReference(src));

                using StringWriter writer = new();

                codeDomProvider.GenerateCodeFromExpression(typeReferenceExpression, writer, new CodeGeneratorOptions());

                //
                // Ezt a nevet meg nem lehet eleresi utvonalakban hasznalni (tartalmaz "<" es ">" karaktereket).
                //

                string unsafeName = writer.GetStringBuilder().ToString();

                return Replacer.Replace(unsafeName, m => m.Groups[0].Value switch
                {
                    "<" => "{",
                    ">" => "}",
                    _ => throw new NotImplementedException()
                });
            });
        }
    }
}
