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
        private UVDecalPlacer _Placer;

        public override string EditorLabel => "UV Decals";
        public override Type EditorIconTargetType => typeof(Texture);

        public UVDecalPlacer Placer => _Placer;
    }

    [Preserve]
    public class UVDecalVisualSwitcherStrategy : AVisualSwitcherStrategy<UVDecalVisualID, UVDecalVisualPresetRecord, UVDecalVisualSwitcherRecord>
    {
        protected override IDisposable Apply(UVDecalVisualPresetRecord presetRecord, UVDecalVisualSwitcherRecord switcherRecord)
        {
            var placer = switcherRecord.Placer;
            if (placer == null)
                return DisposableUtils.Empty;

            placer.SetDecal(presetRecord.Decal);
            return DisposableUtils.Empty;
        }
    }
}
