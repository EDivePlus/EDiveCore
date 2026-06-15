// Author: Michal Petr
// Created: 15.06.2026

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EDIVE.EditorUtils
{
    public static class SerializedObjectUtils
    {
        public static IEnumerable<T> GetSerializedPropertiesOfType<T>(Component root, bool enterSelf = true) where T : class
        {
            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null)
                    continue;

                using var serializedObject = new SerializedObject(component);
                var iterator = serializedObject.GetIterator();
                var enterChildren = true;
                while (iterator.Next(enterChildren))
                {
                    if (iterator.propertyType == SerializedPropertyType.Generic
                        && iterator.type == typeof(T).Name
                        && iterator.boxedValue is T value)
                    {
                        yield return value;
                        if (!enterSelf) enterChildren = false; // No need to descend into a matched property's own properties.
                    }
                    else
                    {
                        enterChildren = true;
                    }
                }
            }
        }
    }
}
