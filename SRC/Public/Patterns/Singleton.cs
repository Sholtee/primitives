﻿/********************************************************************************
* Singleton.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Reflection;

namespace Solti.Utils.Primitives.Patterns
{
    /// <summary>
    /// Implements the Singleton design pattern.
    /// </summary>
    public abstract class Singleton<TConcrete> where TConcrete : Singleton<TConcrete>, new()
    {
        private static readonly object FLock = new();
        private static TConcrete? FValue;

        /// <summary>
        /// The singleton instance.
        /// </summary>
        #pragma warning disable CA1000 // Do not declare static members on generic types
        public static TConcrete Instance
        #pragma warning restore CA1000
        {
            get 
            {
                if (FValue is null)
                    lock (FLock)
                        #pragma warning disable CA1508 // Avoid dead conditional code
                        if (FValue is null)
                        #pragma warning restore CA1508
                            try
                            {
                                FValue = new TConcrete();
                            }

                            //
                            // "new TConcrete()" valojaban Activator.CreateInstance() hivassa fordul (mondjuk erdekes miert)
                            //

                            catch (TargetInvocationException ex)
                            {
                                #pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
                                throw ex.InnerException;
                                #pragma warning restore CA1065
                            }
                return FValue;
            }
        }
    }
}
