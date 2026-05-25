// Author: Michal Petr
// Created: 25.05.2026

using System;
using UnityEngine;

namespace EDIVE.Localization.LocalizeStringModifiers
{
    [Serializable]
    public class PrefixSuffixLocalizeStringModifier : ILocalizeStringModifier
    {
        [SerializeField]
        private string _Prefix = "";
        
        [SerializeField]
        private string _Suffix = "";
        
        public string Apply(string input)
        {
            return $"{_Prefix}{input}{_Suffix}";
        }
    }
}
