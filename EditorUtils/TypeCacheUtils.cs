using System;
using System.Collections.Generic;
using UnityEditor;

namespace EDIVE.EditorUtils
{
    public static class TypeCacheUtils
    {
        public static IEnumerable<T> GetDerivedClassesOfType<T>(params object[] constructorArgs) where T : class
        {
            foreach (var type in TypeCache.GetTypesDerivedFrom<T>())
            {
                if (!type.IsAbstract && type.GetConstructor(Type.EmptyTypes) != null) 
                    yield return (T) Activator.CreateInstance(type, constructorArgs);
            }
        }
    }
}
