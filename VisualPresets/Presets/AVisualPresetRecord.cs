// Author: František Holubec
// Created: 10.11.2025

using System;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.VisualPresets.VisualIDs;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.VisualPresets.Presets
{
    [Serializable]
    public abstract class AVisualPresetRecord : IEquatable<AVisualPresetRecord>
    {
        public abstract string EditorLabel { get; }
        public abstract ABaseVisualID BaseVisualID { get; }
        public virtual bool IsValid() => BaseVisualID != null;

        public virtual bool Equals(AVisualPresetRecord other)
        {
            return other != null && Equals(BaseVisualID, other.BaseVisualID);
        }

        public override int GetHashCode()
        {
            return BaseVisualID != null ? BaseVisualID.GetHashCode() : 0;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((AVisualPresetRecord) obj);
        }
    }
    
    [Serializable]
    public abstract class AVisualPresetRecord<TVisualID> : AVisualPresetRecord where TVisualID : ABaseVisualID
    {
        [HideLabel]
        [ShowCreateNew]
        [JsonProperty("ID")]
        [SerializeField]
        protected TVisualID _VisualID;
        
        public TVisualID VisualID => _VisualID;
        public override ABaseVisualID BaseVisualID => _VisualID;
        
        
    }
}
