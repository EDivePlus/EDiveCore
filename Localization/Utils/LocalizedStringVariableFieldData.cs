// Author: František Holubec
// Created: 30.06.2026

using System;
using EDIVE.DataStructures.VariableFields;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Localization.Utils
{
    [Serializable]
    [TypeRegistryItem("Localized String")]
    public class LocalizedStringVariableFieldData : IVariableFieldData<string>
    {
        [SerializeField]
        private SafeLocalizedString _Value = new();
        
        public string Value
        {
            get => _Value.ToString();
            set => _Value = SafeLocalizedString.EmptyWithFallback(value);
        }
    }
}
