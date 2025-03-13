using System;
using System.Collections.Generic;
using System.Reflection;

namespace EDIVE.NativeUtils
{
    public static class ReflectionExtensions
    {
        public static bool TryGetValue<T>(this FieldInfo fieldInfo, object target, out T result)
        {
            result = default;
            if (fieldInfo == null)
                return false;

            if (target == null)
                return false;

            var value = fieldInfo.GetValue(target);
            if (value is not T tValue)
                return false;

            result = tValue;
            return true;
        }

        public static bool TrySetValue<T>(this FieldInfo fieldInfo, object target, T value)
        {
            if (fieldInfo == null)
                return false;

            if (target == null)
                return false;

            fieldInfo.SetValue(target, value);
            return true;
        }
        
        public static Type GetMostSpecificType(this IEnumerable<Type> types)
        {
            if (types == null)
                return null;

            Type mostSpecific = null;
            foreach (var type in types)
            {
                if (type == null)
                    return null;

                mostSpecific ??= type;

                var assignableFrom = mostSpecific.IsAssignableFrom(type);
                var assignableTo = type.IsAssignableFrom(mostSpecific);

                if (assignableFrom || assignableTo)
                {
                    if (assignableFrom)
                        mostSpecific = type;
                }
                else
                {
                    return null;
                }
            }
            return mostSpecific;
        }
    }
}
