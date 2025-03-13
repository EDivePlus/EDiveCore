using System;
using System.Diagnostics;

namespace EDIVE.OdinExtensions.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    [Conditional("UNITY_EDITOR")]
    public class ListItemRangeIndexAttribute : Attribute
    {
        public int Min;
        public int Max;

        public string MinGetter;
        public string MaxGetter;

        public ListItemRangeIndexAttribute(int max, int min = 0)
        {
            Min = min;
            Max = max;
        }

        public ListItemRangeIndexAttribute(string maxGetter, string minGetter = null)
        {
            MinGetter = minGetter;
            MaxGetter = maxGetter;
        }
    }
}
