using System;
using System.Collections.Generic;
using System.Linq;
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

        public static IEnumerable<T> GetAssignableClassesOfType<T>(params object[] constructorArgs) where T : class
        {
            foreach (var type in GetAssignableTypes<T>())
            {
                if (!type.IsAbstract && type.GetConstructor(Type.EmptyTypes) != null)
                    yield return (T) Activator.CreateInstance(type, constructorArgs);
            }
        }

        public static IEnumerable<Type> GetAssignableTypes<T>()
        {
            return TypeCache.GetTypesDerivedFrom<T>().Append(typeof(T)).Where(type => !type.IsAbstract && !type.IsGenericType);
        }

        public static IEnumerable<Type> GetAssignableTypes(Type targetType)
        {
            return TypeCache.GetTypesDerivedFrom(targetType).Append(targetType).Where(type => !type.IsAbstract && !type.IsGenericType);
        }
    }
}
