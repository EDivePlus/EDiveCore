using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.StateHandling;
using EDIVE.StateHandling.StateValuePresets;
using EDIVE.Tweening.Segments;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EDIVE.Tweening.StateHandling
{
    [Serializable]
    public class ObjectReferenceStateTweenSegment : ACallbackTweenSegment, IPresetTweenSegment
    {
        [SerializeField]
        [ListDrawerSettings(ShowFoldout = false)]
        private List<ObjectReferenceValuePresetBundle> _ObjectPresets = new();

        protected override TweenCallback GetCallbackAction()
        {
            TweenCallback result = null;
            foreach (var objectPreset in _ObjectPresets)
            {
                if (objectPreset == null)
                {
                    Debug.LogError("Object preset is null!");
                    continue;
                }

                if (!objectPreset.Target.TryGetTempValue(out var target))
                    continue;

                TweenCallback applyAction = () => objectPreset.ApplyTo(target);
                if (result == null)
                    result = applyAction;
                else
                    result += applyAction;
            }
            return result;
        }

        public ObjectReferenceStateTweenSegment() { }
        public ObjectReferenceStateTweenSegment(List<ObjectReferenceValuePresetBundle> objectPresets)
        {
            _ObjectPresets = objectPresets;
        }

#if UNITY_EDITOR
        public override void PopulateReferences(HashSet<TweenObjectReference> references)
        {
            base.PopulateReferences(references);
            foreach (var preset in _ObjectPresets) references.Add(preset.Target);
        }

        public bool TryConvertToDirectSegment(out IDirectTweenSegment result, IDictionary<TweenObjectReference, Object> targets)
        {
            var presets = _ObjectPresets
                .Select(o => new ObjectStatePresetRecord(
                    targets.TryGetValue(o.Target, out var target) ? target : null,
                    o.ValuePresets.Select(v => v.GetCopy()).ToList()))
                .ToList();
            result = new ObjectStateTweenSegment(presets);
            return true;
        }

        public override string GetSummary()
        {
            return $"{_Operation.ToString().Nicify()} State Preset";
        }

        public override string LabelName => "State Preset";
#endif
    }

    [Serializable]
    public class ObjectReferenceValuePresetBundle
    {
        [Required]
        [ShowCreateNew]
        [SerializeField]
        private TweenObjectReference _Target;

        [SerializeReference]
        [HideReferenceObjectPicker]
        [EnhancedValidate("ValidateValuePresets", ContinuousValidationCheck = true)]
        [ValueDropdown("GetValuePresetDropdown", IsUniqueList = true, DrawDropdownForListElements = false)]
        internal List<AStateValuePreset> _ValuePresets = new();

        public TweenObjectReference Target => _Target;
        public List<AStateValuePreset> ValuePresets => _ValuePresets;

        public void ApplyTo(Object target)
        {
            if (_ValuePresets == null)
                return;

            foreach (var valuePreset in _ValuePresets)
            {
                if (valuePreset == null)
                {
                    Debug.LogError("Name: " + target.name + ", Value preset is null!");
                    continue;
                }
                valuePreset.ApplyTo(target);

            }
#if UNITY_EDITOR
            EditorUtility.SetDirty(target);
#endif
        }

        public ObjectReferenceValuePresetBundle() { }
        public ObjectReferenceValuePresetBundle(TweenObjectReference target, List<AStateValuePreset> valuePresets)
        {
            _Target = target;
            _ValuePresets = valuePresets;
        }

#if UNITY_EDITOR
        [UsedImplicitly]
        private IEnumerable<ValueDropdownItem<AStateValuePreset>> GetValuePresetDropdown()
        {
            return _Target ? StateControlEditorUtils.GetValuePresetDropdown(_Target.ValueType) : Enumerable.Empty<ValueDropdownItem<AStateValuePreset>>();
        }

        [UsedImplicitly]
        private void ValidateValuePresets(List<AStateValuePreset> value, SelfValidationResult result)
        {
            if (_Target == null) return;
            StateControlEditorUtils.ValidateStateValuePresets(_Target.ValueType, value, result);
        }
#endif
    }
}
