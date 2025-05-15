// Author: František Holubec
// Created: 19.04.2024

using System;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.UIElements.Selectables
{
    [Serializable]
    public class SelectableAdditionalData
    {
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
#endif
    }
}
