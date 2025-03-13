#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EDIVE.DataStructures;
using EDIVE.EditorUtils;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.Tweening.Segments;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.Tweening
{
    public class TweenPresetCreatorWindow : OdinEditorWindow
    {
        [SerializeField]
        [EnhancedTableList(AlwaysExpanded = true)]
        private List<ComponentReference> _References;

        [InlineProperty]
        [SerializeReference]
        [HideReferenceObjectPicker]
        private List<ITweenSegment> _OriginalSequence = new();

        public void Initialize(List<ITweenSegment> segments)
        {
            _OriginalSequence = segments;
            var targets = new TweenTargetCollection();
            foreach (var segment in segments) segment.PopulateTargets(targets);
            _References = targets.Targets
                .Where(t => t.Key != null)
                .Select(t => new ComponentReference(t.Key, t.Value))
                .ToList();
        }

        [Button]
        public void Confirm()
        {
            var convertedSegments = _OriginalSequence
                .ConvertToPresetSegments(_References.ToDictionary(r => r.Value, r => r.Reference))
                .ToList();

            EditorHelper.ExecuteNextFrame(() =>
            {
                var result = OdinExtensionUtils.CreateNewInstanceOfType<TweenAnimationPreset>();
                if (result == null) return;
                result.Initialize(convertedSegments);
                EditorUtility.SetDirty(result);
                EditorGUIUtility.PingObject(result);
            });
        }
        
        [Serializable]
        public class ComponentReference
        {
            [EnableGUI]
            [ReadOnly]
            [SerializeField]
            private Object _Value;

            [EnhancedValueDropdown(nameof(GetAllValidReferences), AppendNextDrawer = true)]
            [ValidateInput(nameof(ValidateReference))]
            [EnableIf(nameof(_Value))]
            [ShowCreateNew(OnCreatedNew = nameof(OnReferenceCreated))]
            [SerializeField]
            private TweenObjectReference _Reference;

            [HideInInspector]
            [SerializeField]
            private UType _RequiredType;

            public Object Value => _Value;
            public TweenObjectReference Reference => _Reference;
            public Type RequiredType => _RequiredType?.Type ?? _Value.GetType();

            public ComponentReference(Object value, Type requiredType)
            {
                _Value = value;
                _RequiredType = requiredType ?? value.GetType();
            }

            private IEnumerable GetAllValidReferences()
            {
                return EditorAssetUtils.FindAllAssetsOfType<TweenObjectReference>()
                    .Where(r => RequiredType.IsAssignableFrom(r.ValueType))
                    .Select(r => new ValueDropdownItem<TweenObjectReference>(r.name, r));
            }

            private bool ValidateReference(TweenObjectReference value, ref string errorMessage, ref InfoMessageType? messageType)
            {
                if (value == null)
                    return true;

                if (value.ValueType == null)
                {
                    errorMessage = "Value type is not defined!";
                    messageType = InfoMessageType.Error;
                    return false;
                }

                if (!RequiredType.IsAssignableFrom(value.ValueType))
                {
                    errorMessage = "Reference type does not match the required type!";
                    messageType = InfoMessageType.Error;
                    return false;
                }
                return true;
            }

            private void OnReferenceCreated(TweenObjectReference value)
            {
                value.ValueType = RequiredType;
            }
        }
    }
}
#endif
