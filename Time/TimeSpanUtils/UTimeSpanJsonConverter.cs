// Author: František Holubec
// Created: 04.04.2025

using System;
using EDIVE.Utils.Json;

namespace EDIVE.Time.TimeSpanUtils
{
    [Serializable]
    public class UTimeSpanJsonConverter : AWrapperJsonConverter<UTimeSpan, TimeSpan>
    {
        protected override UTimeSpan CreateWrapper(TimeSpan value) => new(value);
        protected override TimeSpan GetValue(UTimeSpan wrapper) => wrapper.Value;
        protected override void SetValue(UTimeSpan wrapper, TimeSpan value) => wrapper.Value = value;
    }
}
