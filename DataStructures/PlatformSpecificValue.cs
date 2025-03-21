using System;
using System.Collections.Generic;
using EDIVE.NativeUtils;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.DataStructures
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class PlatformSpecificValue<TValue>
    {
        [JsonProperty("Default")]
        [SerializeField]
        private TValue _DefaultValue;

        [JsonProperty("Records")]
        [SerializeField]
        private List<PlatformSpecificValueRecord> _Records = new();

        public TValue GetCurrentPlatformValue() => GetPlatformValue(PlatformUtils.GetCurrentBuildTarget());
        public TValue GetPlatformValue(RuntimePlatform platform) => TryGetSpecificPlatformValue(platform, out var result) ? result : _DefaultValue;

        public bool TryGetSpecificPlatformValue(RuntimePlatform platform, out TValue result)
        {
            if (_Records.TryGetFirst(rec => rec.Platforms.Contains(platform), out var record))
            {
                result = record.Value;
                return true;
            }

            DebugLite.LogError("No value found for platform: " + platform);
            result = default;
            return false;
        }

        [Serializable]
        [JsonObject(MemberSerialization.OptIn)]
        private class PlatformSpecificValueRecord
        {
            [JsonProperty("Value")]
            [SerializeField]
            private TValue _Value;

            [SerializeField]
            [ListDrawerSettings(ShowFoldout = false)]
            [JsonProperty("Platforms")]
            private List<RuntimePlatform> _Platforms = new();

            public TValue Value => _Value;
            public List<RuntimePlatform> Platforms => _Platforms;
        }
    }
}
