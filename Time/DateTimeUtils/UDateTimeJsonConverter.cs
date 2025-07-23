// Author: František Holubec
// Created: 04.04.2025

using System;
using EDIVE.Time.DateTimeUtils;
using EDIVE.Utils.Json;

namespace EDIVE.DataStructures.DateTimeStructures
{
    public class UDateTimeJsonConverter : AWrapperJsonConverter<UDateTime, DateTime>
    {
        protected override UDateTime CreateWrapper(DateTime value) => new(value);
        protected override DateTime GetValue(UDateTime wrapper) => wrapper.Value;
        protected override void SetValue(UDateTime wrapper, DateTime value) => wrapper.Value = value;
    }
}
