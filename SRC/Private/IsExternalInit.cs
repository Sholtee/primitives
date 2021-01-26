/********************************************************************************
* IsExternalInit.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
#if NETSTANDARD
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// https://stackoverflow.com/questions/62648189/testing-c-sharp-9-0-in-vs2019-cs0518-isexternalinit-is-not-defined-or-imported
    /// </summary>
    internal class IsExternalInit { }
}
#endif
