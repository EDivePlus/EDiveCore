using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.Utilities;
using UnityEditor;

namespace EDIVE.EditorUtils
{
    public static class TypeCacheUtils
    {
        public static IEnumerable<T> GetDerivedClassesOfType<T>() where T : class
        {
            foreach (var type in TypeCache.GetTypesDerivedFrom<T>())
            {
                if (type.GetConstructor(Type.EmptyTypes) != null)
                    yield return (T) Activator.CreateInstance(type);
            }
        }
        
        public static IEnumerable<T> GetDerivedClassesOfType<T>(params object[] constructorArgs) where T : class
        {
            var argTypes = constructorArgs.Select(a => a.GetType()).ToArray();
            foreach (var type in TypeCache.GetTypesDerivedFrom<T>())
            {
                var ctor = type.GetConstructor(argTypes);
                if (ctor != null)
                    yield return (T) Activator.CreateInstance(type, constructorArgs);
            }
        }
        
        public static IEnumerable<T> GetAssignableClassesOfType<T>() where T : class
        {
            foreach (var type in GetAssignableTypes<T>())
            {
                if (type.GetConstructor(Type.EmptyTypes) != null)
                    yield return (T) Activator.CreateInstance(type);
            }
        }

        public static IEnumerable<T> GetAssignableClassesOfType<T>(params object[] constructorArgs) where T : class
        {
            var argTypes = constructorArgs.Select(a => a.GetType()).ToArray();
            foreach (var type in GetAssignableTypes<T>())
            {
                var ctor = type.GetConstructor(argTypes);
                if (ctor != null)
                    yield return (T) Activator.CreateInstance(type, constructorArgs);
            }
        }

        public static IEnumerable<Type> GetAssignableTypes<T>()
        {
            return TypeCache.GetTypesDerivedFrom<T>().Append(typeof(T)).Where(type => !type.IsAbstract && !type.IsGenericType);
        }

        public static IEnumerable<Type> GetAssignableTypes(Type targetType)
        {
            var result = TypeCache.GetTypesDerivedFrom(targetType)
                .Append(targetType)
                .Where(type => !type.IsAbstract && !type.IsGenericType)
                .ToList();

            // TypeCache.GetTypesDerivedFrom on a constructed generic (e.g. IFoo<Bar>) does not return
            // open-generic implementers (e.g. RawFoo<>), since those implement the open IFoo<T>. Query the
            // open definition and close each candidate over our type args. This mirrors Odin's own
            // TypeRegistry.GetInheritors / TryGetCompatibleClosedGenericType.
            if (targetType.IsConstructedGenericType)
            {
                var typeArgs = targetType.GetGenericArguments();
                var openTarget = targetType.GetGenericTypeDefinition();
                foreach (var type in TypeCache.GetTypesDerivedFrom(openTarget))
                {
                    if (type.IsAbstract || !type.IsGenericTypeDefinition)
                        continue;
                    if (type.TryInferGenericParameters(out var inferredParams, typeArgs))
                    {
                        var closed = type.MakeGenericType(inferredParams);
                        if (targetType.IsAssignableFrom(closed) && !result.Contains(closed))
                            result.Add(closed);
                    }
                }
            }

            return result;
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
