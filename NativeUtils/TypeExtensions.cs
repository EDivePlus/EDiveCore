using System;
using System.Linq;
using System.Text;

namespace EDIVE.NativeUtils
{
    public static class TypeExtensions
    {
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
