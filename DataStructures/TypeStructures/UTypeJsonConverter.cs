// Author: František Holubec
// Created: 04.04.2025

using System;
using EDIVE.Utils.Json;

namespace EDIVE.DataStructures.TypeStructures
{
    public class UTypeJsonConverter : AWrapperJsonConverter<UType, Type>
    {
        protected override UType CreateWrapper(Type value) => new(value);
        protected override Type GetValue(UType wrapper) => wrapper.Value;
        protected override void SetValue(UType wrapper, Type value) => wrapper.Value = value;
    }
}
