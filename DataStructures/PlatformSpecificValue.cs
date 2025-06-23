using System;
using System.Collections;
using System.Collections.Generic;
using EDIVE.NativeUtils;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.DataStructures
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class PlatformSpecificValue<TValue> : IEnumerable
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

        public void Add(TValue value, RuntimePlatform platform)
        {
            if (_Records.TryGetFirst(r => Equals(r.Value, value), out var existingRecord))
            {
                if (!existingRecord.Platforms.Contains(platform))
                {
                    existingRecord.Platforms.Add(platform);
                }
                return;
            }

            _Records.Add(new PlatformSpecificValueRecord(value, platform));
        }

        public void Add(TValue value, params RuntimePlatform[] platforms)
        {
            if (_Records.TryGetFirst(r => Equals(r.Value, value), out var existingRecord))
            {
                foreach (var platform in platforms)
                {
                    if (!existingRecord.Platforms.Contains(platform))
                    {
                        existingRecord.Platforms.Add(platform);
                    }
                }

                return;
            }

            _Records.Add(new PlatformSpecificValueRecord(value, platforms));
        }

        public IEnumerator GetEnumerator() => _Records.GetEnumerator();

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
            private List<RuntimePlatform> _Platforms;

            public TValue Value => _Value;
            public List<RuntimePlatform> Platforms => _Platforms;

            public PlatformSpecificValueRecord()
            {
                _Platforms = new List<RuntimePlatform>();
            }

            public PlatformSpecificValueRecord(TValue value, RuntimePlatform platform)
            {
                _Value = value;
                _Platforms = new List<RuntimePlatform> {platform};
            }

            public PlatformSpecificValueRecord(TValue value, params RuntimePlatform[] platforms)
            {
                _Value = value;
                _Platforms = new List<RuntimePlatform>(platforms);
            }
        }
    }
}
