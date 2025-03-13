using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using EDIVE.StateHandling;
using EDIVE.Tweening.Segments;
using EDIVE.NativeUtils;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.Tweening.StateHandling
{
    [Serializable]
    public class ObjectStateTweenSegment : ACallbackTweenSegment, IDirectTweenSegment
    {
        [SerializeField]
        [InlineProperty]
        [HideLabel]
        private ObjectStatePresetField _StatePreset;

        protected override TweenCallback GetCallbackAction() => CallbackAction;

        private void CallbackAction()
        {
            _StatePreset?.Apply();
        }

        public ObjectStateTweenSegment() { }
        public ObjectStateTweenSegment(List<ObjectStatePresetRecord> statePresets)
        {
            _StatePreset = new ObjectStatePresetField(statePresets);
        }

#if UNITY_EDITOR
        public bool TryConvertToPresetSegment(out IPresetTweenSegment result, IDictionary<Object, TweenObjectReference> references)
        {
            var presets = _StatePreset.ObjectPresets
                .Select(o => new ObjectReferenceValuePresetBundle(
                    references.TryGetValue(o.Target, out var reference) ? reference : null,
                    o.ValuePresets.Select(v => v.GetCopy()).ToList()))
                .ToList();
            result = new ObjectReferenceStateTweenSegment(presets);
            return true;
        }

        public override void PopulateTargets(TweenTargetCollection targets)
        {
            base.PopulateTargets(targets);
            foreach (var preset in _StatePreset.ObjectPresets)
            {
                targets.Add(preset.Target, preset.ValuePresets.Select(p => p.TargetType).GetMostSpecificType());
            }
        }

        public override string GetSummary()
        {
            return $"{_Operation.ToString().Nicify()} State Preset";
        }

        public override string LabelName => "State Preset";
#endif
    }
}
