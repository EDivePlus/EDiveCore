// Author: František Holubec
// Created: 13.10.2026

using System;
using EDIVE.NativeUtils;
using EDIVE.VisualPresets.Presets;
using EDIVE.VisualPresets.Switchers;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace
namespace EDIVE.UIElements.Selectables
{
    [Serializable]
    public class SelectableTransitionVisualPresetRecord : AVisualPresetRecord<SelectableTransitionVisualID>
    {
        [VerticalGroup("Value")]
        [SerializeField]
        private TweenSelectableTransitionPreset _Preset;

        public TweenSelectableTransitionPreset Preset => _Preset;

        public override string EditorLabel => "Selectable Transition";

        public SelectableTransitionVisualPresetRecord() { }
        public SelectableTransitionVisualPresetRecord(SelectableTransitionVisualID visualID, TweenSelectableTransitionPreset preset) : base(visualID) { _Preset = preset; }

        public override bool EqualsInternal(AVisualPresetRecord other)
        {
            return other is SelectableTransitionVisualPresetRecord transitionRecord && _Preset == transitionRecord._Preset;
        }

        public override int GetHashCodeInternal()
        {
            return _Preset != null ? _Preset.GetHashCode() : 0;
        }
    }

    // Target needs a TweenSelectableTransition as its custom transition
    [Serializable]
    public class SelectableTransitionVisualSwitcherRecord : AVisualSwitcherRecord<SelectableTransitionVisualID>
    {
        [VerticalGroup("Value")]
        [SerializeField]
        private Selectable _Selectable;

        public Selectable Selectable => _Selectable;

        public override string EditorLabel => "Selectable Transition";
        public override Type EditorIconTargetType => typeof(Button);
        public override UnityEngine.Object Target => _Selectable;
    }

    [Preserve]
    public class SelectableTransitionVisualSwitcherStrategy : AVisualSwitcherStrategy<SelectableTransitionVisualID, SelectableTransitionVisualPresetRecord, SelectableTransitionVisualSwitcherRecord>
    {
        protected override IDisposable Apply(SelectableTransitionVisualPresetRecord presetRecord, SelectableTransitionVisualSwitcherRecord switcherRecord)
        {
            if (!TryGetTransition(switcherRecord, out var selectable, out var transition))
                return DisposableUtils.Empty;

            transition.Preset = presetRecord.Preset;
            selectable.RefreshVisuals();
            return DisposableUtils.Empty;
        }

        protected override SelectableTransitionVisualPresetRecord Capture(SelectableTransitionVisualSwitcherRecord switcherRecord)
        {
            return TryGetTransition(switcherRecord, out _, out var transition) ? new SelectableTransitionVisualPresetRecord(switcherRecord.VisualID, transition.Preset) : null;
        }

        private static bool TryGetTransition(SelectableTransitionVisualSwitcherRecord switcherRecord, out IEnhancedSelectable selectable, out TweenSelectableTransition transition)
        {
            selectable = switcherRecord.Selectable as IEnhancedSelectable;
            transition = selectable?.AdditionalData.CustomTransition as TweenSelectableTransition;
            return transition != null;
        }
    }
}
