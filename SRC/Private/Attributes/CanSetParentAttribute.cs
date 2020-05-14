/********************************************************************************
* CanSetParentAttribute.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Primitives
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal sealed class CanSetParentAttribute: Attribute
    {
    }
}
