using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.Validation;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.Tweening.Segments
{
    [Serializable]
    public class PresetTweenSegment : ABaseTweenSegment, IDirectTweenSegment, IPresetTweenSegment, IParentTweenSegment
    {
        [ShowCreateNew]
        [EnhancedValidate("ValidatePreset")]
        [CustomValueDrawer("CustomPresetDrawer")]
        [InlineIconButton(FontAwesomeEditorIconType.ShareFromSquareSolid, "ExtractPresetButtonAction", "Extract Preset", ShowIf = "ShowExtractButton")]
        [SerializeField]
        private TweenAnimationPreset _Preset;

        public override void AddToSequence(Sequence sequence)
        {
            if (_Preset == null) return;
            AddToSequence(sequence, _Preset.CreateSequence());
        }

        public IEnumerable<ITweenSegment> GetChildSegments()
        {
            return _Preset != null ? _Preset.GetSegments() : Enumerable.Empty<ITweenSegment>();
        }

#if UNITY_EDITOR
        [UsedImplicitly]
        private TweenAnimationPreset CustomPresetDrawer(TweenAnimationPreset value, GUIContent label, Func<GUIContent, bool> callNextDrawer)
        {
            callNextDrawer(label);
            return this.DetectCycle() ? null : value;
        }

        [UsedImplicitly]
        private void ValidatePreset(ValidationResult result, InspectorProperty property)
        {
            if (_Preset == null)
                return;

            if (this.DetectCycle(out var cyclePath))
            {
                var presetPath = cyclePath.OfType<PresetTweenSegment>().Select(p => p._Preset.name);
                result.AddError($"Cyclic reference detected: {string.Join("-", presetPath)}");
            }
        }

        public bool TryConvertToDirectSegment(out IDirectTweenSegment result, IDictionary<TweenObjectReference, Object> targets)
        {
            result = this.GetCopy();
            return true;
        }

        public bool TryConvertToPresetSegment(out IPresetTweenSegment result, IDictionary<Object, TweenObjectReference> references)
        {
            result = this.GetCopy();
            return true;
        }

        [UsedImplicitly]
        public void ExtractPresetButtonAction(InspectorProperty property)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Extract elements"), false, OnValueSelected, false);
            menu.AddItem(new GUIContent("Extract as sequence"), false, OnValueSelected, true);
            var dropdownPosition = new Rect(Event.current.mousePosition, Vector2.zero);
            menu.DropDown(dropdownPosition);

            return;
            void OnValueSelected(object item) => ExtractPreset(property, (bool) item);
        }

        [UsedImplicitly]
        public void ExtractPreset(InspectorProperty property, bool asSequence)
        {
            if (_Preset == null || !property.TryGetParentObject<List<ITweenSegment>>(out var parentList))
                return;

            property.RecordForUndo("Extract Preset");
            var index = parentList.IndexOf(this);
            if (property.HasParentObject<ITweenPreset>())
            {
                var segmentsCopy = _Preset.GetSegments().Select(s => s.GetCopy());
                parentList.RemoveAt(index);
                if (asSequence)
                {
                    var tweenSequence = new TweenSequence(segmentsCopy.ToList());
                    parentList.Insert(index, new SequenceTweenSegment(tweenSequence, _Operation, _InsertionPosition));
                }
                else
                {
                    parentList.InsertRange(index, segmentsCopy);
                }
            }
            else
            {
                var references = property.TryGetParentObject<ITweenReferencesHolder>(out var holder) ? holder.GetReferencesDictionary() : null;
                references ??= new Dictionary<TweenObjectReference, Object>();
                var convertedSegments = _Preset.GetSegments().ConvertToDirectSegments(references);
                parentList.RemoveAt(index);
                if (asSequence)
                {
                    var tweenSequence = new TweenSequence(convertedSegments.ToList());
                    parentList.Insert(index, new SequenceTweenSegment(tweenSequence, _Operation, _InsertionPosition));
                }
                else
                {
                    parentList.InsertRange(index, convertedSegments);
                }
            }
        }

        [UsedImplicitly]
        private bool ShowExtractButton(InspectorProperty property)
        {
            return _Preset != null && property.HasParentObject<List<ITweenSegment>>();
        }

        public override string GetSummary()
        {
            return $"{GetSummaryPrefix()} Preset {(_Preset != null ? _Preset.name : "null")}";
        }

        public override string LabelName => "Preset";

        public override void PopulateReferences(HashSet<TweenObjectReference> references)
        {
            base.PopulateReferences(references);
            if (_Preset == null) return;
            _Preset.PopulateReferences(references);
        }
#endif
    }
}
