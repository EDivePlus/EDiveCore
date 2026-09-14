using System;
using System.Globalization;
using System.Text;
using EDIVE.OdinExtensions.Attributes;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Core.Versions
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    [EnhancedInlineProperty]
    public class AppVersionFormat : ADigitVersionFormat
    {
        public static AppVersionFormat Default => new();
        
        [PropertyOrder(0)]
        [SerializeField]
        [JsonProperty("Prefix")]
        private string _Prefix = "v.";

        public string Prefix { get => _Prefix; set => _Prefix = value; }

        public AppVersionFormat() { }
        public AppVersionFormat(string prefix, int[] digits) : base(digits)
        {
            _Prefix = prefix;
        }

        public string Format(AppVersion version)
        {
            var stringBuilder = new StringBuilder(20);

            if (!string.IsNullOrEmpty(_Prefix))
                stringBuilder.Append(_Prefix);

            var isFirstSegment = true;
            foreach (var significance in AppVersionSignificanceUtils.ALL)
            {
                var digits = GetDigitsAt(significance);
                if (digits <= 0)
                    continue;

                if (!isFirstSegment)
                    stringBuilder.Append('.');

                stringBuilder.Append(version.GetSegment(significance).ToString($"D{digits}", CultureInfo.InvariantCulture));
                isFirstSegment = false;
            }
            return stringBuilder.ToString();
        }

#if UNITY_EDITOR
        protected override void ValidateDigits(int[] value, SelfValidationResult result)
        {
            base.ValidateDigits(value, result);
            
            var hiddenIndex = -1;
            var shownCount = 0;
            for (var i = 0; i < AppVersionSignificanceUtils.SEGMENT_COUNT; i++)
            {
                if (GetDigitsAt(AppVersionSignificanceUtils.ALL[i]) == 0)
                {
                    if (hiddenIndex < 0)
                        hiddenIndex = i;
                    continue;
                }

                shownCount++;
                if (hiddenIndex >= 0)
                {
                    result.AddWarning($"{AppVersionSignificanceUtils.GetLabel(i)} is shown while {AppVersionSignificanceUtils.GetLabel(hiddenIndex)} before it is hidden");
                    hiddenIndex = -1;
                }
            }

            if (shownCount == 0)
                result.AddError("All segments are hidden, the version string would be empty");
        }
#endif
    }
}
