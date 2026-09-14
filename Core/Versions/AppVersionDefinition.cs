using System;
using System.Collections.Generic;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using System.Globalization;
using UnityEditor;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
#endif

namespace EDIVE.Core.Versions
{
    public class AppVersionDefinition : ScriptableObject
    {
        [PropertyOrder(-10)]
        [SerializeField]
        [EnhancedValidate("ValidateCurrentVersion", ContinuousValidationCheck = true)]
        private AppVersion _CurrentVersion;
        
        [SerializeField]
        private bool _AutoIncrement = true;
        
        [ShowIf(nameof(_AutoIncrement))]
        [Indent]
        [SerializeField]
        private AppVersionSignificance _IncrementSignificance = AppVersionSignificance.Build;

        [SerializeField]
        [PropertySpace]
        [PropertyOrder(10)]
        [FormerlySerializedAs("_Formating")]
        [EnhancedValidate("ValidateFormatting")]
        [OnValueChanged("OnFormattingChanged", true)]
        [ListDrawerSettings(DraggableItems = false, OnTitleBarGUI = "OnTitleBarGUI")]
        private List<FormattingRecord> _Formatting = new();

        public AppVersion CurrentVersion
        {
            get => _CurrentVersion;
            set
            {
                _CurrentVersion = value;
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }
        }

        public string DisplayVersionString => CurrentDisplayFormat.Format(CurrentVersion);
        public string StoreVersionString => CurrentVersion.ToStoreString();
        public int BundleCode => CurrentBundleCodeFormat.GetCode(CurrentVersion);

        public AppVersionFormat CurrentDisplayFormat => GetDisplayFormat(CurrentVersion);
        public BundleCodeFormat CurrentBundleCodeFormat => GetBundleCodeFormat(CurrentVersion);

        public AppVersionFormat GetDisplayFormat(AppVersion version)
        {
            return TryGetFormattingRecord(version, out var record) ? record.DisplayFormat : AppVersionFormat.Default;
        }

        public BundleCodeFormat GetBundleCodeFormat(AppVersion version)
        {
            return TryGetFormattingRecord(version, out var record) ? record.BundleCodeFormat : BundleCodeFormat.Default;
        }

        private bool TryGetFormattingRecord(AppVersion version, out FormattingRecord record)
        {
            return _Formatting.TryGetLast(r => r.FromVersion <= version, out record);
        }

        public static implicit operator AppVersion(AppVersionDefinition versionDefinition) => versionDefinition.CurrentVersion;

        [Serializable]
        public class FormattingRecord : IComparable<FormattingRecord>
        {
            [SerializeField]
            private AppVersion _FromVersion = AppVersion.ZERO;

            [SerializeField]
            private AppVersionFormat _DisplayFormat = AppVersionFormat.Default;

            [SerializeField]
            private BundleCodeFormat _BundleCodeFormat = BundleCodeFormat.Default;

            public AppVersion FromVersion => _FromVersion;
            public AppVersionFormat DisplayFormat => _DisplayFormat;
            public BundleCodeFormat BundleCodeFormat => _BundleCodeFormat;

            public int CompareTo(FormattingRecord other)
            {
                if (ReferenceEquals(this, other)) return 0;
                if (other is null) return 1;
                return Comparer<AppVersion>.Default.Compare(_FromVersion, other._FromVersion);
            }

#if UNITY_EDITOR
            [PropertyOrder(-1)]
            [OnInspectorGUI]
            public void DrawPreview(InspectorProperty property)
            {
                if (!property.TryGetParentObject<AppVersionDefinition>(out var def))
                    return;

                var version = def.CurrentVersion;

                GUIHelper.PushGUIEnabled(false);
                SirenixEditorGUI.BeginHorizontalPropertyLayout(GUIHelper.TempContent("Preview"));
                GUILayout.Label(GUIHelper.TempContent(_DisplayFormat.Format(version), "Display Version"), EditorStyles.numberField);
                GUILayout.Label(GUIHelper.TempContent(version.ToStoreString(), "Store Version"), EditorStyles.numberField);
                GUILayout.Label(GUIHelper.TempContent(_BundleCodeFormat.GetCode(version).ToString(), "Bundle Code"), EditorStyles.numberField);
                SirenixEditorGUI.EndHorizontalPropertyLayout();
                GUIHelper.PopGUIEnabled();
            }
#endif
        }

#if UNITY_EDITOR
        [UsedImplicitly]
        private void ValidateCurrentVersion(AppVersion value, SelfValidationResult result)
        {
            var bundleFormat = GetBundleCodeFormat(value);
            foreach (var significance in AppVersionSignificanceUtils.ALL)
            {
                var segment = value.GetSegment(significance);
                if (segment == 0)
                    continue;

                var digits = bundleFormat.GetDigitsAt(significance);
                if (digits == 0)
                    result.AddWarning($"{significance} ({segment}) is dropped from the bundle code, no digits are reserved for it");
                else if (segment > bundleFormat.GetMaxValueAt(significance))
                    result.AddError($"{significance} ({segment}) does not fit into the {digits} digit(s) reserved for it, the bundle code is clamped to {bundleFormat.GetMaxValueAt(significance)}");
            }

            var bundleCode = bundleFormat.GetCode(value);
            if (bundleCode > BundleCodeFormat.MAX_ANDROID_BUNDLE_CODE)
                result.AddError($"Bundle code {bundleCode} exceeds the Android limit of {BundleCodeFormat.MAX_ANDROID_BUNDLE_CODE}");
        }

        [UsedImplicitly]
        private void ValidateFormatting(List<FormattingRecord> value, SelfValidationResult result)
        {
            if (value == null || value.Count == 0)
            {
                result.AddWarning("No formatting records defined, default formatting is used for every version");
                return;
            }

            if (value.Exists(r => r == null))
            {
                result.AddError("Formatting records contain a null entry")
                    .WithFix(() => value.RemoveAll(r => r == null));
                return;
            }

            if (value[0].FromVersion > AppVersion.ZERO)
                result.AddWarning($"Versions below {value[0].FromVersion} fall back to the default formatting");

            for (var i = 1; i < value.Count; i++)
            {
                if (value[i - 1].FromVersion <= value[i].FromVersion)
                    continue;

                result.AddError("Formatting records must be ordered by ascending 'From Version', otherwise the wrong record is picked")
                    .WithFix(value.Sort);
                return;
            }

            for (var i = 1; i < value.Count; i++)
            {
                var boundary = value[i].FromVersion;
                if (boundary == value[i - 1].FromVersion)
                {
                    result.AddWarning($"Duplicate 'From Version' {boundary}, only the last of the duplicates is ever used");
                    continue;
                }

                var previousCode = value[i - 1].BundleCodeFormat.GetCode(boundary);
                var currentCode = value[i].BundleCodeFormat.GetCode(boundary);
                if (currentCode <= previousCode)
                    result.AddWarning($"Bundle code does not grow across the {boundary} boundary ({previousCode} -> {currentCode}), stores reject non-increasing bundle codes");
            }
        }
        
        [PropertyOrder(-1)]
        [OnInspectorGUI]
        public void DrawPreview(InspectorProperty property)
        {
            GUILayout.Space(2);
            GUIHelper.PushGUIEnabled(false);
            SirenixEditorGUI.BeginHorizontalPropertyLayout(GUIHelper.TempContent("Preview"));
            GUILayout.Label(GUIHelper.TempContent(DisplayVersionString, "Display Version"), EditorStyles.numberField);
            GUILayout.Label(GUIHelper.TempContent(StoreVersionString, "Store Version"), EditorStyles.numberField);
            GUILayout.Label(GUIHelper.TempContent(BundleCode.ToString(), "Bundle Code"), EditorStyles.numberField);
            SirenixEditorGUI.EndHorizontalPropertyLayout();
            GUIHelper.PopGUIEnabled();
        }

        public void TryIncrementCurrentVersion()
        {
            if (!_AutoIncrement)
                return;
            CurrentVersion = CurrentVersion.Incremented(_IncrementSignificance);
        }

        [Button]
        public void ApplyToSettings()
        {
            var bundleCode = BundleCode;
            PlayerSettings.bundleVersion = StoreVersionString;
            PlayerSettings.Android.bundleVersionCode = bundleCode;
            PlayerSettings.iOS.buildNumber = bundleCode.ToString(CultureInfo.InvariantCulture);
        }

        [UsedImplicitly]
        private void OnTitleBarGUI(InspectorProperty property)
        {
            if (SirenixEditorGUI.ToolbarButton(FontAwesomeEditorIcons.ArrowDownShortWideSolid))
            {
                _Formatting.Sort();
                property.MarkSerializationRootDirty();
            }
        }

        [UsedImplicitly]
        private void OnFormattingChanged(InspectorProperty property)
        {
            _Formatting.Sort();
            property.MarkSerializationRootDirty();
        }
#endif
    }
}
