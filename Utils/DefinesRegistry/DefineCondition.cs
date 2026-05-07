// Author: František Holubec
// Created: 07.05.2026

using System;
using EDIVE.Conditions;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.Utils.DefinesRegistry
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class DefineCondition : ABoolCondition
    {
        [SerializeField]
        [JsonProperty("Define")]
        private string _Define;

        protected override bool GetValue() => ActiveDefinesRegistry.IsDefined(_Define);
    }
}
