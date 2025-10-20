// Author: František Holubec
// Created: 20.10.2025

using System;
using Newtonsoft.Json;

namespace EDIVE.Conditions
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class ConstantCondition : ABoolCondition
    {
        protected override bool GetValue() => true;
    }
}
