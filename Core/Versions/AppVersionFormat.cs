using System;
using System.Collections.Generic;
using EDIVE.OdinExtensions.Attributes;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Core.Versions
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class AppVersionFormat
    {
        public static readonly AppVersionFormat DEFAULT = new();

        [LabelWidth(60)]
        [HorizontalGroup]
        [Range(1, 4)]
        [SerializeField]
        [JsonProperty("Major")]
        private int _Depth = 4;

        [LabelWidth(60)]
        [HorizontalGroup]
        [SerializeField]
        [JsonProperty("Prefix")]
        private string _Prefix = "v.";
        
        [LabelWidth(120)]
        [PropertyTooltip("Each number defines minimum amount of digits generated for respective version segment")]
        [InlineList(ElementSuffixGetter = "$GetLeadingZeroSuffix")]
        [RequiredListLength(4)]
        [SerializeField]
        [JsonProperty("LeadingZeros")]
        private int[] _LeadingZeros = {0, 2, 2, 3};

        public int Depth { get => _Depth; set => _Depth = value; }

        public string Prefix { get => _Prefix; set => _Prefix = value; }

        public int[] LeadingZeros { get => _LeadingZeros; set => _LeadingZeros = value; }

        public AppVersionFormat() { }
        public AppVersionFormat(int depth, string prefix, int[] leadingZeros)
        {
            _Depth = depth;
            _Prefix = prefix;
            _LeadingZeros = leadingZeros;
        }
        
        public IEnumerable<AppVersionSignificance> GetSignificances()
        {
            yield return AppVersionSignificance.Major;
            if (Depth >= 2) yield return AppVersionSignificance.Minor;
            if (Depth >= 3) yield return AppVersionSignificance.Patch;
            if (Depth >= 4) yield return AppVersionSignificance.Build;
        }

        public int GetZerosAt(AppVersionSignificance significance)
        {
            var index = (int) significance;
            if (LeadingZeros != null && LeadingZeros.Length > index)
            {
                return Mathf.Clamp(LeadingZeros[index], 0, 3);
            }

            return 0;
        }

#if UNITY_EDITOR
        [UsedImplicitly]
        private string GetLeadingZeroSuffix(int index)
        {
            return index switch
            {
                0 => "Major",
                1 => "Minor",
                2 => "Patch",
                3 => "Build",
                _ => ""
            };
        }
#endif
        
    }
}
