/********************************************************************************
* Assembly.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Resources;
using System.Runtime.CompilerServices;

[
assembly:
    NeutralResourcesLanguage("en"),
#if DEBUG
    InternalsVisibleTo("Solti.Utils.Primitives.Tests"),
    InternalsVisibleTo("DynamicProxyGenAssembly2"), // Moq
#endif
]