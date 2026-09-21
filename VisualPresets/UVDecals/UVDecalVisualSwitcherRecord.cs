// Author: Michal Petr
// Created: 21.09.2026

using System;
using EDIVE.NativeUtils;
using EDIVE.Rendering.UVDecals;
using EDIVE.VisualPresets.Switchers;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;

namespace EDIVE.VisualPresets.UVDecals
{
    [Serializable]
    public class UVDecalVisualSwitcherRecord : AVisualSwitcherRecord<UVDecalVisualID>
    {
        [VerticalGroup("Value")]
        [SerializeField]
        private UVDecalPainter _Painter;

        public override string EditorLabel => "UV Decals";
        public override Type EditorIconTargetType => typeof(Texture);

        public UVDecalPainter Painter => _Painter;
    }

    [Preserve]
    public class UVDecalVisualSwitcherStrategy : AVisualSwitcherStrategy<UVDecalVisualID, UVDecalVisualPresetRecord, UVDecalVisualSwitcherRecord>
    {
        protected override IDisposable Apply(UVDecalVisualPresetRecord presetRecord, UVDecalVisualSwitcherRecord switcherRecord)
        {
            var painter = switcherRecord.Painter;
            if (painter == null)
                return DisposableUtils.Empty;

            if (presetRecord.Decal == null)
                painter.Clear();
            else
                painter.SetDecals(presetRecord.Decal.Decals);

            return DisposableUtils.Empty;
        }
    }
}
