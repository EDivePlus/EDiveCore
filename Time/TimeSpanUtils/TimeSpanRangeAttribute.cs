using System;
using System.Diagnostics;

namespace EDIVE.Time.TimeSpanUtils
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class TimeSpanRangeAttribute : Attribute
    {
        public string MinGetter;
        public string MaxGetter;

        public TimeUnit SnappingUnit;
        public bool Inline;

        public string DisableMinMaxIf;

        public TimeSpanRangeAttribute(string maxGetter, bool inline = false, TimeUnit snappingUnit = TimeUnit.Seconds)
        {
            MaxGetter = maxGetter;
            SnappingUnit = snappingUnit;
            Inline = inline;
        }

        public TimeSpanRangeAttribute(string minGetter, string maxGetter, bool inline = false, TimeUnit snappingUnit = TimeUnit.Seconds)
        {
            MinGetter = minGetter;
            MaxGetter = maxGetter;
            SnappingUnit = snappingUnit;
            Inline = inline;
        }
    }
}
