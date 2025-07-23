﻿using System;
using System.Diagnostics;

namespace EDIVE.Time.TimeSpanUtils
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class TimeSpanDrawerSettingsAttribute : Attribute
    {
        public TimeUnit HighestUnit = TimeUnit.Days;
        public TimeUnit LowestUnit = TimeUnit.Seconds;

        public TimeSpanDrawerSettingsAttribute() { }

        public TimeSpanDrawerSettingsAttribute(TimeUnit highestUnit, TimeUnit lowestUnit)
        {
            HighestUnit = highestUnit;
            LowestUnit = lowestUnit;
        }

        public TimeSpanDrawerSettingsAttribute(TimeUnit highestUnit, bool drawMilliseconds = false)
        {
            HighestUnit = highestUnit;
            LowestUnit = drawMilliseconds ? TimeUnit.Milliseconds : TimeUnit.Seconds;
        }

        public TimeSpanDrawerSettingsAttribute(bool drawMilliseconds)
        {
            HighestUnit = TimeUnit.Days;
            LowestUnit = drawMilliseconds ? TimeUnit.Milliseconds : TimeUnit.Seconds;
        }
    }
}
