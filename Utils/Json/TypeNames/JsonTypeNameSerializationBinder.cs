// Author: František Holubec
// Created: 15.06.2025

using System.Collections.Generic;
using System.Reflection;
using EDIVE.NativeUtils;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace EDIVE.Utils.Json.TypeNames
{
    public class JsonTypeNameSerializationBinder : DefaultSerializationBinder
    {
        private readonly Dictionary<string, System.Type> _formerNameToType = new();
        private readonly Dictionary<string, System.Type> _nameToType = new();
        private readonly Dictionary<System.Type, string> _typeToName;

        public JsonTypeNameSerializationBinder()
        {
            try
            {
                var attributeAssembly = typeof(JsonTypeNameAttribute).Assembly;
                var attributeAssemblyName = attributeAssembly.GetName().Name;
                var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();

                foreach (var assembly in assemblies)
                {
                    if (assembly.IsDynamic || assembly.IsNonUserAssembly())
                        continue;

                    if (assembly != attributeAssembly && !assembly.IsReferencingAssembly(attributeAssemblyName))
                        continue;

                    try
                    {
                        var types = assembly.GetExportedTypes();

                        foreach (var type in types)
                        {
                            var jsonTypeNameAttribute = type.GetCustomAttribute<JsonTypeNameAttribute>(false);
                            if (jsonTypeNameAttribute != null)
                            {
                                RegisterTypeName(type, jsonTypeNameAttribute.TypeName);
                            }

                            var formerJsonTypeNameAttribute = type.GetCustomAttribute<FormerJsonTypeNameAttribute>(false);
                            if (formerJsonTypeNameAttribute != null)
                            {
                                RegisterFormerTypeName(type, formerJsonTypeNameAttribute.TypeName);
                            }
                        }
                    }
                    catch (ReflectionTypeLoadException e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }

        public override void BindToName(System.Type serializedType, out string assemblyName, out string typeName)
        {
            if (_typeToName.TryGetValue(serializedType, out var name))
            {
                assemblyName = null;
                typeName = name;
                return;
            }

            base.BindToName(serializedType, out assemblyName, out typeName);
        }

        public override System.Type BindToType(string assemblyName, string typeName)
        {
            if (_nameToType.TryGetValue(typeName, out var type) || _formerNameToType.TryGetValue(typeName, out type))
            {
                return type;
            }
            
            return base.BindToType(assemblyName, typeName);
        }

        public void RegisterFormerTypeName(System.Type type, string name)
        {
            _formerNameToType[name] = type;
        }

        public void RegisterTypeName(System.Type type, string name)
        {
#if UNITY_EDITOR
            if (_nameToType.ContainsKey(name))
            {
                Debug.LogError($"[TypeNameBinder] Multiple types have JsonTypeNameAttribute with value '{name}' ('{type.FullName}', '{_nameToType.GetValueOrDefault(name).FullName}')");
            }

            if (_typeToName.ContainsKey(type))
            {
                Debug.LogWarning($"[TypeNameBinder] Type '{type.Name}' already has a JsonTypeName defined - is the same type being processed multiple times? ('{type.FullName}')");
            }
#endif
            _typeToName[type] = name;
            _nameToType[name] = type;
        }
    }
}
