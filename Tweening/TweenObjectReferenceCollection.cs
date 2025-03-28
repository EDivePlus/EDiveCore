using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
#endif

namespace EDIVE.Tweening
{
    [Serializable]
    [InlineProperty]
    [HideLabel]
    public class TweenObjectReferenceCollection
    {
        [SerializeField]
        [LabelText("@$property.Parent.NiceName")]
        [EnhancedTableList(IsReadOnly = true, OnTitleBarGUI = "OnPresetsTitleBarGUI")]
        private List<ReferencePreset> _Presets = new();

        public TweenObjectReferenceCollection() { }
        public TweenObjectReferenceCollection(List<ReferencePreset> presets)
        {
            _Presets = presets;
        }

        public void AssignTempReferences()
        {
            foreach (var preset in _Presets)
            {
                preset.AssignTempReferenceValue();
            }
        }

        public void ClearTempReferences()
        {
            foreach (var preset in _Presets)
            {
                preset.ClearTempReferenceValue();
            }
        }

        public bool TryGetPreset(TweenObjectReference reference, out ReferencePreset preset)
        {
            return _Presets.TryGetFirst(p => p.Reference == reference, out preset);
        }

        public IDictionary<TweenObjectReference, Object> GetReferencesDictionary()
        {
            return _Presets.ToDictionary(p => p.Reference, p => p.Value);
        }

#if UNITY_EDITOR
        private ITweenTargetProvider _provider;
        private InspectorProperty _providerProperty;

        [OnInspectorInit]
        private void OnInspectorInit(InspectorProperty property)
        {
            if (!property.TryGetParentObject(out _provider, out _providerProperty))
                return;

            _providerProperty.ValueEntry.OnChildValueChanged += OnProviderChanged;
            RefreshPresets(false);
        }

        [OnInspectorDispose]
        private void OnInspectorDispose(InspectorProperty property)
        {
            if (!property.TryGetParentObject(out _provider, out _providerProperty))
                return;

            _providerProperty.ValueEntry.OnChildValueChanged -= OnProviderChanged;
        }

        private void OnProviderChanged(int obj)
        {
            RefreshPresets();
        }

        private void RefreshPresets(bool markDirty = true)
        {
            var references = new HashSet<TweenObjectReference>();
            _provider.PopulateReferences(references);

            _Presets = references.Where(r => r != null)
                .Select(r => TryGetPreset(r, out var p) ? p : new ReferencePreset(r))
                .ToList();

            if (markDirty)
                _providerProperty.MarkSerializationRootDirty();
        }

        private void OnPresetsTitleBarGUI()
        {
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
            {
                RefreshPresets();
            }
        }
#endif

        [Serializable]
        public class ReferencePreset
        {
            [EnableGUI]
            [ReadOnly]
            [SerializeField]
            private TweenObjectReference _Reference;

            [CustomValueDrawer("CustomValueDrawer")]
            [SerializeField]
            private Object _Value;

            public TweenObjectReference Reference => _Reference;
            public Object Value => _Value;

            public ReferencePreset(TweenObjectReference reference)
            {
                _Reference = reference;
            }

            public ReferencePreset(TweenObjectReference reference, Object value)
            {
                _Reference = reference;
                _Value = value;
            }

            public void AssignTempReferenceValue()
            {
                if (_Reference is null) return;
                _Reference.SetTempValue(_Value);
            }

            public void ClearTempReferenceValue()
            {
                _Reference.ClearTempValue();
            }

            protected bool Equals(ReferencePreset other)
            {
                return Equals(_Reference, other._Reference);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((ReferencePreset) obj);
            }

            public override int GetHashCode()
            {
                return (_Reference != null ? _Reference.GetHashCode() : 0);
            }

#if UNITY_EDITOR
            [UsedImplicitly]
            private Object CustomValueDrawer(Object value, GUIContent label, Func<GUIContent, bool> callNextDrawer)
            {
                if (_Reference == null || _Reference.ValueType == null)
                {
                    callNextDrawer(label);
                    return value;
                }

                return SirenixEditorFields.UnityObjectField(label, value, _Reference.ValueType, true);
            }
#endif
        }
    }
}
