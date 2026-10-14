// Author: František Holubec
// Created: 14.10.2026

using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.View.EditorPreview
{
    public enum PreviewDeviceType
    {
        Any,
        Handheld,
        Desktop
    }

    // Part of the screen the edited prefab is laid out in, e.g. above the taskbar
    [Serializable]
    public class PreviewContentArea
    {
        [SerializeField]
        private string _Name = "Window";

        [SerializeField]
        private bool _InSafeArea = true;

        [Tooltip("Keeps clear of placeholders that reserve space")]
        [SerializeField]
        private bool _AvoidPlaceholders = true;

        [Tooltip("Left, bottom, right, top")]
        [SerializeField]
        private Vector4 _Padding;

        public string Name => _Name;
        public bool InSafeArea => _InSafeArea;
        public bool AvoidPlaceholders => _AvoidPlaceholders;
        public Vector4 Padding => _Padding;

        public PreviewContentArea() { }
        public PreviewContentArea(string name, bool inSafeArea, bool avoidPlaceholders)
        {
            _Name = name;
            _InSafeArea = inSafeArea;
            _AvoidPlaceholders = avoidPlaceholders;
        }
    }

    // Host stand-ins shown around UI prefabs in Prefab Mode
    [CreateAssetMenu(menuName = "EDIVE/View/Preview Environment Preset")]
    public class PreviewEnvironmentPreset : ScriptableObject
    {
        [Tooltip("Auto picks this preset for these devices")]
        [SerializeField]
        private PreviewDeviceType _DeviceType;

        [Tooltip("Auto prefers this preset when the device model contains this text")]
        [SerializeField]
        private string _DeviceModel;

        [SerializeField]
        private bool _OverrideScaler;

        [ShowIf(nameof(_OverrideScaler))]
        [SerializeField]
        private Vector2 _ReferenceResolution = new(1280, 720);

        [ShowIf(nameof(_OverrideScaler))]
        [Range(0, 1)]
        [SerializeField]
        private float _MatchWidthOrHeight = 1;

        [SerializeReference]
        private List<APreviewPlaceholder> _Placeholders = new();

        [Tooltip("The first one is the default")]
        [SerializeField]
        private List<PreviewContentArea> _Areas = new() { new PreviewContentArea() };

        public bool OverrideScaler => _OverrideScaler;
        public Vector2 ReferenceResolution => _ReferenceResolution;
        public float MatchWidthOrHeight => _MatchWidthOrHeight;
        public IReadOnlyList<APreviewPlaceholder> Placeholders => _Placeholders;
        public IReadOnlyList<PreviewContentArea> Areas => _Areas;

        // Named area, else the first one
        public PreviewContentArea GetArea(string areaName)
        {
            return _Areas.FirstOrDefault(a => a != null && a.Name == areaName) ?? _Areas.FirstOrDefault(a => a != null) ?? new PreviewContentArea("Full", true, false);
        }

        // Higher fits better, -1 does not fit
        public int GetMatchScore(string deviceModel, DeviceType deviceType)
        {
            if (!string.IsNullOrWhiteSpace(_DeviceModel))
                return deviceModel != null && deviceModel.Contains(_DeviceModel, StringComparison.OrdinalIgnoreCase) ? 2 : -1;

            return _DeviceType switch
            {
                PreviewDeviceType.Any => 0,
                PreviewDeviceType.Handheld => deviceType == DeviceType.Handheld ? 1 : -1,
                PreviewDeviceType.Desktop => deviceType == DeviceType.Desktop ? 1 : -1,
                _ => -1
            };
        }

        private void OnValidate() => PreviewEnvironment.SetDirty();
    }
}
