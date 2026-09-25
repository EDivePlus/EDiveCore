// Author: František Holubec
// Created: 25.09.2026

#if PURRNET
using System;
using EDIVE.Rendering.UVDecals;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PurrNet;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.VisualPresets.UVDecals
{
    // Asset refs saved as PurrNet network asset persistent IDs
    public class UVDecalPresetJsonConverter : JsonConverter<UVDecalPreset>
    {
        public override void WriteJson(JsonWriter writer, UVDecalPreset value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var json = new JObject
            {
                ["Placement"] = GetAssetID(value._Placement),
                ["Texture"] = GetAssetID(value._Texture),
                ["Tint"] = new JArray(value._Tint.r, value._Tint.g, value._Tint.b, value._Tint.a),
                ["OverrideSmoothness"] = value._OverrideSmoothness,
                ["Smoothness"] = value._Smoothness,
                ["Scale"] = new JArray(value._Scale.x, value._Scale.y),
                ["Anchor"] = new JArray(value._Anchor.x, value._Anchor.y),
                ["Pivot"] = new JArray(value._Pivot.x, value._Pivot.y),
                ["Rotation"] = value._Rotation,
            };
            json.WriteTo(writer);
        }

        public override UVDecalPreset ReadJson(JsonReader reader, Type objectType, UVDecalPreset existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var json = JObject.Load(reader);
            var preset = new UVDecalPreset
            {
                _Placement = GetAsset<UVDecalPlacement>(json["Placement"]),
                _Texture = GetAsset<Texture2D>(json["Texture"]),
                _OverrideSmoothness = json.Value<bool?>("OverrideSmoothness") ?? false,
                _Smoothness = json.Value<float?>("Smoothness") ?? 0f,
                _Rotation = json.Value<float?>("Rotation") ?? 0f,
            };
            if (json["Tint"] is JArray { Count: 4 } tint)
                preset._Tint = new Color(tint[0].Value<float>(), tint[1].Value<float>(), tint[2].Value<float>(), tint[3].Value<float>());
            if (TryReadVector2(json["Scale"], out var scale))
                preset._Scale = scale;
            if (TryReadVector2(json["Anchor"], out var anchor))
                preset._Anchor = anchor;
            if (TryReadVector2(json["Pivot"], out var pivot))
                preset._Pivot = pivot;
            return preset;
        }

        private static JToken GetAssetID(Object asset)
        {
            if (asset == null)
                return JValue.CreateNull();
            var manager = NetworkManager.main;
            if (manager != null && manager.TryGetNetworkAssetPersistentId(asset, out var id))
                return id;
            Debug.LogWarning($"[UVDecal] '{asset.name}' is not a network asset, not saved.");
            return JValue.CreateNull();
        }

        private static T GetAsset<T>(JToken token) where T : Object
        {
            var id = token?.Type == JTokenType.String ? token.Value<string>() : null;
            if (string.IsNullOrEmpty(id))
                return null;
            var manager = NetworkManager.main;
            if (manager != null && manager.TryGetNetworkAssetByPersistentId(id, out var asset))
                return asset as T;
            Debug.LogWarning($"[UVDecal] Network asset '{id}' not found.");
            return null;
        }

        private static bool TryReadVector2(JToken token, out Vector2 value)
        {
            value = default;
            if (token is not JArray { Count: 2 } array)
                return false;
            value = new Vector2(array[0].Value<float>(), array[1].Value<float>());
            return true;
        }
    }
}
#endif
