using System;
using System.Collections.Generic;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
#endif

namespace EDIVE.Core.Versions
{
    public class AppVersionDefinition : ScriptableObject
    {
        [SerializeField]
        private AppVersion _CurrentVersion;
        
        [SerializeField]
        [OnValueChanged("OnFormatingChanged", true)]
        [ListDrawerSettings(DraggableItems = false, OnTitleBarGUI = "OnTitleBarGUI")]
        private List<FormatingRecord> _Formating = new();

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

        public string VersionString => CurrentVersion.GetFormatedString(CurrentVersionFormat);
        public int BundleCode => CurrentVersion.GetBundleCode(CurrentBundleCodeFormat);
        
        public AppVersionFormat CurrentVersionFormat => GetVersionFormat(CurrentVersion);
        public BundleCodeFormat CurrentBundleCodeFormat => GetBundleCodeFormat(CurrentVersion);
        
        public AppVersionFormat GetVersionFormat(AppVersion version)
        {
            return TryGetFormatingRecord(version, out var record) ? record.Format : AppVersionFormat.DEFAULT;
        }

        public BundleCodeFormat GetBundleCodeFormat(AppVersion version)
        {
            return TryGetFormatingRecord(version, out var record) ? record.BundleCodeFormat : BundleCodeFormat.DEFAULT;
        }
        
        private bool TryGetFormatingRecord(AppVersion version, out FormatingRecord record)
        {
            return _Formating.TryGetLast(r => r.FromVersion <= version, out record);
        }
        
        public static implicit operator AppVersion(AppVersionDefinition versionDefinition) => versionDefinition.CurrentVersion;

        [Serializable]
        public class FormatingRecord : IComparable<FormatingRecord>
        {
            [SerializeField]
            private AppVersion _FromVersion = AppVersion.ZERO;
        
            [SerializeField]
            private AppVersionFormat _Format = AppVersionFormat.DEFAULT;
        
            [SerializeField]
            private BundleCodeFormat _BundleCodeFormat = BundleCodeFormat.DEFAULT;

            public AppVersion FromVersion => _FromVersion;
            public AppVersionFormat Format => _Format;
            public BundleCodeFormat BundleCodeFormat => _BundleCodeFormat;

            public int CompareTo(FormatingRecord other)
            {
                if (ReferenceEquals(this, other)) return 0;
                if (other is null) return 1;
                return Comparer<AppVersion>.Default.Compare(_FromVersion, other._FromVersion);
            }
        }
        
#if UNITY_EDITOR
        public void IncrementCurrentVersion()
        {
            CurrentVersion.Build += 1;
            EditorUtility.SetDirty(this);
        }

        public void ApplyCurrentVersion() => CurrentVersion.Apply(CurrentVersionFormat, CurrentBundleCodeFormat);

        [UsedImplicitly]
        private void OnTitleBarGUI(InspectorProperty property)
        {
            if (SirenixEditorGUI.ToolbarButton(FontAwesomeEditorIcons.ArrowDownShortWideSolid))
            {
                _Formating.Sort();
                property.MarkSerializationRootDirty();
            } 
        }
        
        [UsedImplicitly]
        private void OnFormatingChanged(InspectorProperty property)
        {
            _Formating.Sort();
            property.MarkSerializationRootDirty();
        }
#endif
    }
}
