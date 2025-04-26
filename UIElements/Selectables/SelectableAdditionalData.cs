// Author: František Holubec
// Created: 19.04.2024

using System;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.Validation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.UIElements.Selectables
{
    [Serializable]
    public class SelectableAdditionalData
    {
        [EnhancedValidate("ValidateCustomTransition")]
        [SerializeReference]
        [InlineProperty]
        [HideLabel]
        [TypeSelectorSettings(ShowNoneItem = true)]
        [BoxGroup("Custom Transition")]
        private ASelectableTransition _CustomTransition;

        public void DoStateTransition(SelectionState state, bool instant = false)
        {
            _CustomTransition?.DoStateTransition(state, instant);
        }

#if UNITY_EDITOR
        private SelectionState _state;


        [EnhancedFoldoutGroup("Preview", "@ColorTools.Orange", SpaceBefore = 6)]
        [HorizontalGroup("Preview/Line")]
        [EnhancedBoxGroup("Preview/Line/Animated", "@ColorTools.Green", CenterLabel = true)]
        [EnumToggleButtons]
        [HideLabel]
        [ShowInInspector]
        [ShowIf(nameof(_CustomTransition))]
        private SelectionState StatePreview
        {
            get => _state;
            set
            {
                _state = value;
                DoStateTransition(_state);
            }
        }

        [EnhancedBoxGroup("Preview/Line/Instant", "@ColorTools.Cyan", CenterLabel = true)]
        [EnumToggleButtons]
        [HideLabel]
        [ShowInInspector]
        [ShowIf(nameof(_CustomTransition))]
        private SelectionState StatePreviewInstant
        {
            get => _state;
            set
            {
                _state = value;
                DoStateTransition(_state, true);
            }
        }

        [UsedImplicitly]
        private void ValidateCustomTransition(ValidationResult result, InspectorProperty property)
        {
            if (_CustomTransition != null && property.TryGetParentObject<Selectable>(out var selectable) && selectable.transition != Selectable.Transition.None)
            {
                result.AddError("Custom transition is not compatible with built-in transition. Please set transition to None.")
                    .WithFix(() =>
                    {
                        selectable.transition = Selectable.Transition.None;
                        EditorUtility.SetDirty(selectable);
                    });
            }
        }
#endif
    }
}
