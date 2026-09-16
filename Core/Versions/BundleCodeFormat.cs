using System;
using System.Linq;
using EDIVE.OdinExtensions.Attributes;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Core.Versions
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    [EnhancedInlineProperty]
    public class BundleCodeFormat : ADigitVersionFormat
    {
        public const int MAX_ANDROID_BUNDLE_CODE = 2100000000;

        public static BundleCodeFormat Default => new();
        
        public int TotalDigits => AppVersionSignificanceUtils.ALL.Sum(GetDigitsAt);

        public long MaxBundleCode
        {
            get
            {
                var max = 0L;
                foreach (var significance in AppVersionSignificanceUtils.ALL)
                {
                    var digits = GetDigitsAt(significance);
                    if (digits <= 0)
                        continue;

                    var factor = Pow10(digits);
                    max = max * factor + (factor - 1);
                }
                return max;
            }
        }

        public BundleCodeFormat() { }
        public BundleCodeFormat(byte[] digits) : base(digits) { }

        public int GetMaxValueAt(AppVersionSignificance significance) => Pow10(GetDigitsAt(significance)) - 1;

        public int GetCode(AppVersion version)
        {
            var code = 0L;
            foreach (var significance in AppVersionSignificanceUtils.ALL)
            {
                var digits = GetDigitsAt(significance);
                if (digits <= 0)
                    continue;

                var factor = Pow10(digits);
                code = code * factor + Mathf.Clamp(version.GetSegment(significance), 0, factor - 1);
            }
            return (int) Math.Min(code, int.MaxValue);
        }

        private static int Pow10(int digits)
        {
            var result = 1;
            for (var i = 0; i < digits; i++)
                result *= 10;
            return result;
        }

#if UNITY_EDITOR
        protected override void ValidateDigits(byte[] value, SelfValidationResult result)
        {
            base.ValidateDigits(value, result);
            
            var maxBundleCode = MaxBundleCode;
            if (maxBundleCode == 0)
                result.AddError("No digits are reserved, every version would produce bundle code 0");
            else if (maxBundleCode > MAX_ANDROID_BUNDLE_CODE)
                result.AddError($"{TotalDigits} reserved digits allow codes up to {maxBundleCode}, above the Google Play limit of {MAX_ANDROID_BUNDLE_CODE}");
        }
#endif
    }
}
