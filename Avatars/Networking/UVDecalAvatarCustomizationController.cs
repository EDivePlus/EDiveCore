// Author: Michal Petr
// Created: 21.09.2026

using System.Collections.Generic;
using System.Linq;
using EDIVE.AssetTranslation;
using EDIVE.NativeUtils;
using EDIVE.VisualPresets.UVDecals;
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
                display.SetSelected(display.Definition == decal, false);
        }

        private void Populate()
        {
            _Container.DestroyChildren();
            _displays.Clear();

            if (_IncludeNone)
                Spawn(null);

            if (!AssetTranslationConfig.Instance.TryGetTranslator<UVDecalDefinitionTranslator>(out var translator))
                return;

            foreach (var definition in translator.BaseDefinitions.OfType<UVDecalDefinition>().OrderBy(d => d.UniqueID))
                Spawn(definition);
        }

        private void Spawn(UVDecalDefinition definition)
        {
            var display = Instantiate(_DisplayPrefab, _Container, false);
            display.name = _DisplayPrefab.name + "_" + (definition != null ? definition.UniqueID : "None");
            display.SetDefinition(definition);
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

            SetRecord(new UVDecalVisualPresetRecord(ChangedVisualID, display.Definition));
        }
    }
}
