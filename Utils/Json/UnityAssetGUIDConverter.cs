using System;
using Newtonsoft.Json;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EDIVE.Utils.Json
{
    public class UnityAssetGuidConverter : JsonConverter
    {
#if UNITY_EDITOR
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var asset = value as UnityEngine.Object;
            var guid = asset ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset)) : null;
            writer.WriteValue(guid);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var guid = reader.Value as string;
            return string.IsNullOrEmpty(guid) ? null : AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(UnityEngine.Object).IsAssignableFrom(objectType);
        } 
#else
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => throw new NotImplementedException();
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) => throw new NotImplementedException();
        public override bool CanConvert(Type objectType) => false;
#endif
    }
}
