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

        public static IEnumerable<Type> GetDerivedGenericTypes<TParent,T>()
        {
            var parentTypes = TypeCache.GetTypesDerivedFrom<TParent>();
            var genericTypes = TypeCache.GetTypesDerivedFrom<T>();
            foreach (var genericType in genericTypes)
            {
                if (genericType.IsAbstract)
                    continue;

                foreach (var parentType in parentTypes)
                {
                    if (parentType.IsAbstract || !parentType.IsGenericType)
                        continue;

                    var arguments = parentType.GetGenericArguments();
                    if (arguments.Length != 1 || !arguments[0].GetGenericParameterConstraints().All(c => c.IsAssignableFrom(genericType)))
                        continue;

                    yield return parentType.MakeGenericType(genericType);
                }
            }
        }

        public static IEnumerable<TParent> GetDerivedClassesOfGenericTypes<TParent, T>(params object[] constructorArgs)
        {
            foreach (var type in GetDerivedGenericTypes<TParent, T>())
            {
                yield return (TParent) Activator.CreateInstance(type, constructorArgs);
            }
        }
    }
}
