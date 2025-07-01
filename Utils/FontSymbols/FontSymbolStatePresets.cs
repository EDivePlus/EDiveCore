// Author: František Holubec
// Created: 18.06.2025

using System;
using EDIVE.StateHandling.StateValuePresets;
using UnityEngine.Scripting;

namespace EDIVE.Utils.FontSymbols
{
    [Serializable, Preserve]
    public class FontSymbolTextSymbolPreset : AStateValuePreset<FontSymbolText, FontSymbol>
    {
        public override string Title => "Font Symbol";
        public override void ApplyTo(FontSymbolText targetObject) => targetObject.FontSymbol = Value;
        public override void CaptureFrom(FontSymbolText targetObject) => Value = targetObject.FontSymbol;
    }

    [Serializable, Preserve]
    public class FontSymbolTMPTextUISymbolPreset : AStateValuePreset<FontSymbolTMPTextUI, FontSymbol>
    {
        public override string Title => "Font Symbol";
        public override void ApplyTo(FontSymbolTMPTextUI targetObject) => targetObject.FontSymbol = Value;
        public override void CaptureFrom(FontSymbolTMPTextUI targetObject) => Value = targetObject.FontSymbol;
    }
}
