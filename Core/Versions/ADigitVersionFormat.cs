using System;
using EDIVE.OdinExtensions.Attributes;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Core.Versions
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class ADigitVersionFormat
    {
        protected static readonly byte[] DEFAULT_DIGITS = {1, 2, 3, 3};

        [PropertyOrder(10)]
        [MinValue(0)]
        [MaxValue(AppVersionSignificanceUtils.MAX_SEGMENT_DIGITS)]
        [PropertyTooltip("Digits per segment, 0 hides it.")]
        [InlineList(ElementSuffixGetter = "$GetDigitsSuffix")]
        [EnhancedValidate("ValidateDigits")]
        [SerializeField]
        [JsonProperty("Digits")]
        protected byte[] _Digits = {1, 2, 3, 3};

        protected ADigitVersionFormat() { }
        protected ADigitVersionFormat(byte[] digits)
        {
            _Digits = digits;
        }

        public virtual int GetDigitsAt(AppVersionSignificance significance)
        {
            var index = (int) significance;
            if (index is < 0 or >= AppVersionSignificanceUtils.SEGMENT_COUNT)
                return 0;

            if (_Digits == null || index >= _Digits.Length)
                return 0;

            return Mathf.Clamp(_Digits[index], 0, AppVersionSignificanceUtils.MAX_SEGMENT_DIGITS);
        }

#if UNITY_EDITOR
        [UsedImplicitly]
        protected virtual void ValidateDigits(byte[] value, SelfValidationResult result)
        {
            if (value == null || value.Length != AppVersionSignificanceUtils.SEGMENT_COUNT)
            {
                result.AddError($"Digits must define exactly {AppVersionSignificanceUtils.SEGMENT_COUNT} values")
                    .WithFix(() => _Digits = (byte[]) DEFAULT_DIGITS.Clone());
            }
        }
        
        [UsedImplicitly]
        protected string GetDigitsSuffix(int index) => AppVersionSignificanceUtils.GetLabel(index);
#endif
    }
}
