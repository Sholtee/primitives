/********************************************************************************
* Assembly.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Resources;
using System.Runtime.CompilerServices;

[
assembly:
    NeutralResourcesLanguage("en"),
    CLSCompliant(true),
#if DEBUG
    InternalsVisibleTo("Solti.Utils.Primitives.Tests"),
    InternalsVisibleTo("DynamicProxyGenAssembly2"), // Moq
#endif
]