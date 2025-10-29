// Author: František Holubec
// Created: 10.11.2025

using System;
using EDIVE.VisualPresets.VisualIDs;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.VisualPresets.Presets
{
    [Serializable]
    public abstract class AVisualPresetRecord
    {
        public abstract ABaseVisualID BaseVisualID { get; }
        public virtual bool IsValid() => BaseVisualID != null;
    }
    
    [Serializable]
    public abstract class AVisualPresetRecord<TVisualID> : AVisualPresetRecord where TVisualID : ABaseVisualID
    {
        [HideLabel]
        [JsonProperty("ID")]
        [SerializeField]
        protected TVisualID _VisualID;
        
        public TVisualID VisualID => _VisualID;
        public override ABaseVisualID BaseVisualID => _VisualID;
    }
}
