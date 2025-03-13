using System;
using System.Linq;
using EDIVE.DataStructures;
using EDIVE.OdinExtensions;
using EDIVE.Utils.ObjectActions;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.Tweening
{
    public class TweenObjectReference : ScriptableObject
    {
        [SerializeField]
        [ValidateInput(nameof(IsValueTypeDefined), "Value type is not defined")]
        [CustomValueDrawer("CustomValueTypeDrawer")]
        private UType _ValueType;

        public Type ValueType
        {
            get => _ValueType.Type;
            internal set
            {
                _ValueType = value;
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }
        }

        public bool IsValueTypeDefined => _ValueType.Type != null;

        private Object _tempValue;
        private bool _hasTempValue;

        public void SetTempValue(Object value)
        {
            if (value == null)
            {
                Debug.LogWarning($"[{nameof(TweenObjectReference)}] Value is null for type {ValueType.Name}");
                _tempValue = null;
                _hasTempValue = false;
                return;
            }

            if (!_ValueType.Type.IsInstanceOfType(value))
            {
                Debug.LogError($"[{nameof(TweenObjectReference)}] {value.GetType().Name} is not compatible with the defined value type {ValueType.Name}");
                _tempValue = null;
                _hasTempValue = false;
                return;
            }

            _tempValue = value;
            _hasTempValue = true;
        }

        public void ClearTempValue()
        {
            _tempValue = null;
            _hasTempValue = false;
        }

        public bool TryGetTempValue(out Object target)
        {
            target = _tempValue;
            return _hasTempValue;
        }

#if UNITY_EDITOR
        [UsedImplicitly]
        private UType CustomValueTypeDrawer(UType value, GUIContent label, Func<GUIContent, bool> callNextDrawer, InspectorProperty property)
        {
            value ??= new UType(null);
            Texture icon = GUIHelper.GetAssetThumbnail(null, value.Type, false);
            TypeSelector.DrawSelectorDropdown(label, GUIHelper.TempContent($" {value.Type?.Name ?? "Undefined"}", icon, value.Type?.FullName), rect =>
            {
                var types = ObjectActionUtils.GetSupportedTargetTypes<IObjectAction>();
                var selector = new TypeSelector(types, false);
                selector.SelectionConfirmed += t =>
                {
                    var type = t.FirstOrDefault();
                    value.Type = type;
                    property.ForceMarkDirty();
                };
                selector.ShowInPopup(rect);
                return selector;
            });
            return value;
        }
#endif
    }
}
