// Author: Michal Petr
// Created: 21.09.2026

using System.Collections.Generic;
using EDIVE.NativeUtils;
using EDIVE.Rendering.UVDecals;
using EDIVE.VisualPresets.UVDecals;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Avatars.Networking
{
    public class UVDecalAvatarCustomizationController : AAvatarCustomizationController<UVDecalVisualID, UVDecalVisualPresetRecord>
    {
        [SerializeField]
        private UVDecalDisplay _DisplayPrefab;

        [SerializeField]
        private Transform _Container;

        [SerializeField]
        private bool _IncludeNone = true;

        [SerializeField]
        [ListDrawerSettings(ShowFoldout = false, ListElementLabelName = nameof(UVDecalPreset.EditorLabel))]
        private List<UVDecalPreset> _Decals = new();

        private readonly List<UVDecalDisplay> _displays = new();

        protected override void Awake()
        {
            Populate();
            base.Awake();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            foreach (var display in _displays)
                display.Selected -= OnDisplaySelected;
        }

        protected override void OnRecordChanged(UVDecalVisualPresetRecord current)
        {
            var decal = current?.Decal;
            foreach (var display in _displays)
                display.SetSelected(Equals(display.Preset, decal), false);
        }

        private void Populate()
        {
            _Container.DestroyChildren();
            _displays.Clear();

            if (_IncludeNone)
                Spawn(null);

            foreach (var preset in _Decals)
                Spawn(preset);
        }

        private void Spawn(UVDecalPreset preset)
        {
            var display = Instantiate(_DisplayPrefab, _Container, false);
            display.name = _DisplayPrefab.name + "_" + (preset?._Texture != null ? preset._Texture.name : "None");
            display.SetPreset(preset);
            display.Selected += OnDisplaySelected;
            _displays.Add(display);
        }

        private void OnDisplaySelected(UVDecalDisplay display)
        {
            foreach (var other in _displays)
            {
                if (other != display)
                    other.SetSelected(false, false);
            }

            SetRecord(new UVDecalVisualPresetRecord(ChangedVisualID, display.Preset));
        }
    }
}
