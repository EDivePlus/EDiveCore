// Author: František Holubec
// Created: 12.03.2025

using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;

namespace EDIVE.Utils.ObjectActions
{
    public static class ObjectActionUtils
    {
#if UNITY_EDITOR
        public static IEnumerable<ValueDropdownItem<TObjectAction>> GetTweenActionsDropdown<TObjectAction>(Type targetType) where TObjectAction : class, IObjectAction
        {
            return GetDerivedClassesOfType<TObjectAction>()
                .Where(a => a.IsValidFor(targetType))
                .Select(a => new ValueDropdownItem<TObjectAction>(a.ToString(), a));
        }

        public static IEnumerable<Type> GetSupportedTargetTypes<TObjectAction>() where TObjectAction : class, IObjectAction
        {
            return GetDerivedClassesOfType<TObjectAction>()
                .Select(t => t.TargetType);
        }
        
        private static IEnumerable<T> GetDerivedClassesOfType<T>(params object[] constructorArgs) where T : class
        {
            foreach (var type in TypeCache.GetTypesDerivedFrom<T>())
            {
                if (!type.IsAbstract && type.GetConstructor(Type.EmptyTypes) != null) 
                    yield return (T) Activator.CreateInstance(type, constructorArgs);
            }
        }
#endif
    }
}
