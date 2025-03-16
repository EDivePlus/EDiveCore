using System;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace EDIVE.Utils.Json
{
    public static class JsonUtils
    {
        public static string PrettifyJsonString(string json)
        {
            try
            {
                return JToken.Parse(json).ToString(Formatting.Indented);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return json;
            }
        }
        
        public static string SimplifyJsonString(string json)
        {
            try
            {
                return JToken.Parse(json).ToString(Formatting.None);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return json;
            }
        }

        public static Type ResolveJsonTypeName(string value, ISerializationBinder binder)
        {
            try
            {
                SplitFullyQualifiedTypeName(value, out var typeName, out var assemblyName);
                return binder.BindToType(assemblyName, typeName);
            }
            catch (Exception ex)
            {
               Debug.LogException(ex);
            }

            return null;
        }

        public static string GetJsonTypeName(Type t, TypeNameAssemblyFormatHandling assemblyFormat, ISerializationBinder binder)
        {
            var qualifiedTypeName = GetFullyQualifiedTypeName(t, binder);
            return assemblyFormat switch
            {
                TypeNameAssemblyFormatHandling.Simple => RemoveAssemblyDetails(qualifiedTypeName),
                TypeNameAssemblyFormatHandling.Full => qualifiedTypeName,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        
        private static string GetFullyQualifiedTypeName(Type t, ISerializationBinder binder)
        {
            if (binder == null) return t.AssemblyQualifiedName;
            binder.BindToName(t, out var assemblyName, out var typeName);
            return typeName + (assemblyName == null ? "" : ", " + assemblyName);
        }
        
        private static string RemoveAssemblyDetails(string fullyQualifiedTypeName)
        {
            var stringBuilder = new StringBuilder();
            var flag1 = false;
            var flag2 = false;
            foreach (var ch in fullyQualifiedTypeName)
            {
                switch (ch)
                {
                    case ',':
                        if (!flag1)
                        {
                            flag1 = true;
                            stringBuilder.Append(ch);
                            break;
                        }
                        flag2 = true;
                        break;
                    case '[':
                    case ']':
                        flag1 = false;
                        flag2 = false;
                        stringBuilder.Append(ch);
                        break;
                    default:
                        if (!flag2)
                        {
                            stringBuilder.Append(ch);
                            break;
                        }
                        break;
                }
            }
            return stringBuilder.ToString();
        }
        
        private static void SplitFullyQualifiedTypeName(string fullyQualifiedTypeName, out string typeName, out string assemblyName)
        {
            var assemblyDelimiterIndex = GetAssemblyDelimiterIndex(fullyQualifiedTypeName);
            if (assemblyDelimiterIndex.HasValue)
            {
                typeName = Trim(fullyQualifiedTypeName, 0, assemblyDelimiterIndex.GetValueOrDefault());
                assemblyName = Trim(fullyQualifiedTypeName, assemblyDelimiterIndex.GetValueOrDefault() + 1, fullyQualifiedTypeName.Length - assemblyDelimiterIndex.GetValueOrDefault() - 1);
            }
            else
            {
                typeName = fullyQualifiedTypeName;
                assemblyName = null;
            }
        }

        private static int? GetAssemblyDelimiterIndex(string fullyQualifiedTypeName)
        {
            var num = 0;
            for (var index = 0; index < fullyQualifiedTypeName.Length; ++index)
            {
                switch (fullyQualifiedTypeName[index])
                {
                    case ',':
                        if (num == 0)
                            return index;
                        break;
                    case '[':
                        ++num;
                        break;
                    case ']':
                        --num;
                        break;
                }
            }
            return null;
        }
        
        
        private static string Trim(string s, int start, int length)
        {
            if (s == null)
                throw new ArgumentNullException();
            if (start < 0)
                throw new ArgumentOutOfRangeException(nameof (start));
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof (length));
            var index = start + length - 1;
            if (index >= s.Length)
                throw new ArgumentOutOfRangeException(nameof (length));
            while (start < index && char.IsWhiteSpace(s[start]))
                ++start;
            while (index >= start && char.IsWhiteSpace(s[index]))
                --index;
            return s.Substring(start, index - start + 1);
        }
        
        public static T JsonCopy<T>(this T obj) where T : class
        {
            return (T) JsonUtility.FromJson(JsonUtility.ToJson(obj), obj.GetType());
        }
    }
}
