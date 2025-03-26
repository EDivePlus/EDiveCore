using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

#if !UNITY_EDITOR
using System.IO;
#endif

namespace EDIVE.Utils.Json
{
    public delegate bool TryGetAssetKeyDelegate<in TAsset>(TAsset asset, out string key) where TAsset : ScriptableObject;
    public delegate bool TryGetAssetDelegate<TAsset>(string key, out TAsset asset) where TAsset : ScriptableObject;

    public static class JsonAssetUtils
    {
        public static JArray SerializeAssets<TAsset>(JsonSerializer serializer, IEnumerable<TAsset> assets, Type baseType = null)
            where TAsset : ScriptableObject
        {
            var resultObject = new JArray();
            foreach (var asset in assets)
            {
                if (asset == null) continue;
                resultObject.Add(SerializeAsset(serializer, asset, baseType));
            }
            return resultObject;
        }

        public static JObject SerializeAssets<TAsset>(JsonSerializer serializer, IEnumerable<TAsset> assets, TryGetAssetKeyDelegate<TAsset> assetKeyGetter, Type baseType = null)
            where TAsset : ScriptableObject
        {
            if (assetKeyGetter == null)
            {
                Debug.LogError("AssetKeyGetter parameter is null!");
                return null;
            }

            var resultObject = new JObject();
            foreach (var asset in assets)
            {
                if (asset == null) continue;
                try
                {
                    if (assetKeyGetter(asset, out var key))
                    {
                        resultObject[key] = SerializeAsset(serializer, asset, baseType);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            return resultObject;
        }

        public static void CreateMissingAssets<TAsset>(JsonSerializer serializer, JObject itemCollectionJObject, Predicate<string> shouldCreatePredicate,
            Action<string, JObject, TAsset> onCreatedCallback)
            where TAsset : ScriptableObject
        {
            if(shouldCreatePredicate == null || onCreatedCallback == null) 
                return;
            
            foreach (var itemProperty in itemCollectionJObject.Properties())
            {
                if (itemProperty.Value is not JObject itemJObject)
                    continue;

                var itemId = itemProperty.Name;
                if (shouldCreatePredicate.Invoke(itemId))
                    continue;

                var scriptableObject = CreateEmptyAsset<TAsset>(serializer, itemJObject);
                onCreatedCallback.Invoke(itemId, itemJObject, scriptableObject);
            }
        }

        private static TAsset CreateEmptyAsset<TAsset>(JsonSerializer serializer, JObject jObject)
            where TAsset : ScriptableObject
        {
            var type = typeof(TAsset);
            if (jObject.TryGetValue("$type", out var typeNameToken))
            {
                var typeName = typeNameToken.Value<string>();
                type = JsonUtils.ResolveJsonTypeName(typeName, serializer.SerializationBinder);
            }
            var resultAsset = ScriptableObject.CreateInstance(type) as TAsset;
            return resultAsset;
        }


        public static void PopulateAssets<TAsset>(JsonSerializer serializer, JObject itemsJson, TryGetAssetDelegate<TAsset> getDefinition)
            where TAsset : ScriptableObject
        {
            foreach (var itemProperty in itemsJson.Properties())
            {
                if (itemProperty.Value is not JObject itemJObject)
                    continue;

                var itemId = itemProperty.Name;
                if (!getDefinition(itemId, out var itemDefinition))
                    continue;

                PopulateAsset(serializer, itemJObject, itemDefinition);
            }
        }

        public static void PopulateAsset<TAsset>(JsonSerializer serializer, JObject jObject, TAsset asset)
            where TAsset : ScriptableObject
        {
#if UNITY_EDITOR
            var originalValue = SerializeAsset(serializer, asset, asset.GetType());
            jObject.Remove("$type");

            if (JsonComparer.DeepEquals(jObject, originalValue))
                return;

            using var reader = jObject.CreateReader();
            serializer.Populate(reader, asset);
            EditorUtility.SetDirty(asset);
#else
            using var jsonReader = jObject.CreateReader();
            serializer.Populate(jsonReader, asset);
#endif
        }

        public static void PopulateAsset<TAsset>(JsonSerializer serializer, string json, TAsset asset)
            where TAsset : ScriptableObject
        {
#if UNITY_EDITOR
            var overrideValue = JObject.Parse(json);
            overrideValue.Remove("$type");
            var originalValue = SerializeAsset(serializer, asset, asset.GetType());

            if (JsonComparer.DeepEquals(overrideValue, originalValue))
                return;

            using var reader = overrideValue.CreateReader();
            serializer.Populate(reader, asset);
            EditorUtility.SetDirty(asset);
#else
            using var stringReader = new StringReader(json);
            using JsonReader jsonReader = new JsonTextReader(stringReader);
            serializer.Populate(jsonReader, asset);
#endif
        }

        public static JObject SerializeAsset<TAsset>(JsonSerializer serializer, TAsset asset, Type baseType = null)
            where TAsset : ScriptableObject
        {
            using var writer = new JTokenWriter();
            serializer.Serialize(writer, asset, baseType);
            if (writer.Token is JObject jObject)
                return jObject;

            UnityAssetConverterUtility.DisableOnce();
            writer.Flush();
            serializer.Serialize(writer, asset, baseType);
            return (JObject) writer.Token;
        }
    }
}
