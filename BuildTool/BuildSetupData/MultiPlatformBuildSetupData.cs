// Author: František Holubec
// Created: 21.03.2025

using System;
using System.Collections.Generic;
using EDIVE.BuildTool.Utils;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace EDIVE.BuildTool.BuildSetupData
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class MultiPlatformBuildSetupData
    {
        [SerializeField]
        [JsonProperty("Records")]
        [LabelText("@$property.Parent.NiceName")]
        private List<PlatformRecord> _Records = new();

        public IEnumerable<SerializedBuildSetupData> GetData(NamedBuildTarget namedBuildTarget, BuildTarget target)
        {
            foreach (var record in _Records)
            {
                if (record.Platforms.ContainsTarget(namedBuildTarget, target))
                    yield return record.Data;
            }
        }

        [Serializable]
        [JsonObject(MemberSerialization.OptIn)]
        private class PlatformRecord
        {
            [SerializeField]
            [JsonProperty("Platforms")]
            private PlatformType _Platforms;

            [PropertySpace(6)]
            [HideLabel]
            [SerializeField]
            [JsonProperty("Data")]
            private SerializedBuildSetupData _Data;

            public SerializedBuildSetupData Data => _Data;
            public PlatformType Platforms => _Platforms;
        }
    }
}
