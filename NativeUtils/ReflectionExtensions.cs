using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

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

        public static bool IsNonUserAssembly(this Assembly assembly)
        {
            var fullname = assembly.FullName.ToLower();
            var cannotStartWith = new[] {"unity", "system.", "mscorlib", "mono.", "microsoft", "google", "firebase", "sirenix", "dotween"};
            return cannotStartWith.Any(c => fullname.StartsWith(c));
        }

        public static bool IsReferencingAssembly(this Assembly assembly, Assembly otherAssembly)
        {
            return assembly.IsReferencingAssembly(otherAssembly.GetName());
        }
        
        public static bool IsReferencingAssembly(this Assembly assembly, AssemblyName otherAssemblyName)
        {
            return assembly.IsReferencingAssembly(otherAssemblyName.Name);
        }
        
        public static bool IsReferencingAssembly(this Assembly assembly, string otherAssemblyName)
        {
            return assembly.GetReferencedAssemblies().Any(a => a.Name == otherAssemblyName);
        }
        
        public static string GetFriendlyName(this Type type)
        {
            var friendlyName = type.Name;
            if (type.IsArray)
            {
                return GetFriendlyName(type.GetElementType()) + "[]";
            }

            if (type.IsGenericType)
            {
                var stringBuilder = new StringBuilder(friendlyName, 60);
                var iBacktick = friendlyName.IndexOf('`');
                if (iBacktick > 0)
                {
                    stringBuilder.Length = iBacktick;
                }

                stringBuilder.Append('<');
                var typeParameters = type.GetGenericArguments();
                for (var i = 0; i < typeParameters.Length; ++i)
                {
                    var typeParamName = GetFriendlyName(typeParameters[i]);
                    if (i > 0) stringBuilder.Append(',');
                    stringBuilder.Append(typeParamName);
                }

                stringBuilder.Append('>');
                return stringBuilder.ToString();
            }

            return friendlyName;
        }

        public static bool HasExplicitAttribute(this Type type, Type attributeType)
        {
            if (type == null || attributeType == null) return false;
            return type.CustomAttributes.Any(a => a.AttributeType == attributeType);
        }
    }
}
