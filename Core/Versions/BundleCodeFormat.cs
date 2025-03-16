using System;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Core.Versions
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class BundleCodeFormat
    {
        public static readonly BundleCodeFormat DEFAULT = new();

        [LabelWidth(80)]
        [HorizontalGroup]
        [SerializeField]
        [PropertyRange(1,3)]
        [JsonProperty("MinorDigits")]
        private int _MinorDigits = 3;
        
        [LabelWidth(80)]
        [HorizontalGroup]
        [SerializeField]
        [PropertyRange(1,3)]
        [JsonProperty("PatchDigits")]
        private int _PatchDigits = 3;
        
        public int MinorDigits => _MinorDigits;
        public int PatchDigits => _PatchDigits;
        
        public BundleCodeFormat() { }
        public BundleCodeFormat(int minorDigits, int patchDigits)
        {
            _MinorDigits = Mathf.Clamp(minorDigits, 0, 3);
            _PatchDigits = Mathf.Clamp(patchDigits, 0, 3);
        }
    }
}
