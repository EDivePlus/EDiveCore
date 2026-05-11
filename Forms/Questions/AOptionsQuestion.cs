// Author: Michal Petr
// Created: 05.11.2025

using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.Extensions.Random;
using EDIVE.OdinExtensions.Attributes;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using EDIVE.OdinExtensions;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
#endif

namespace EDIVE.Forms.Questions
{
    [Serializable]
    public abstract class AOptionsQuestion : AFormQuestion
    {
        [SerializeField]
        [MinMaxSlider(0, "@OptionCount", true)]
        [EnhancedValidate("ValidateSelectionLimits")]
        private Vector2Int _SelectionLimits = new(1, 1);
        
        [UsedImplicitly]
        private int OptionCount => BaseOptions.Count();
        public abstract IEnumerable<IQuestionOption> BaseOptions { get; }
        public Vector2Int SelectionLimits => _SelectionLimits;

        protected AOptionsQuestion() { }
        protected AOptionsQuestion(string id) : base(id) { }

#if UNITY_EDITOR
        [UsedImplicitly]
        private void ValidateSelectionLimits(SelfValidationResult result, InspectorProperty property)
        {
            if (_SelectionLimits.y < 1)
                result.AddError("No options can be selected.")
                    .WithFix(() =>
                    {
                        _SelectionLimits.y = 1;
                        property.MarkSerializationRootDirty();
                    });

            var correctCount = BaseOptions.Count(o => o.IsCorrect);
            if (_SelectionLimits.y < correctCount)
                result.AddError($"Minimum lower than correct options count ({correctCount})")
                    .WithFix(() =>
                    {
                        _SelectionLimits.y = correctCount;
                        property.MarkSerializationRootDirty();
                    });
        }
#endif
    }

    [Serializable]
    public abstract class AOptionsQuestion<TOption> : AOptionsQuestion where TOption : IQuestionOption
    {
        [EnhancedTableList(OnTitleBarGUI = "OnOptionsTitleBarGUI")]
        [SerializeField]
        private List<TOption> _Options = new();

        public IReadOnlyList<TOption> Options => _Options;
        public override IEnumerable<IQuestionOption> BaseOptions => _Options.Cast<IQuestionOption>();

        protected AOptionsQuestion() { }

        protected AOptionsQuestion(string id, List<TOption> options) : base(id)
        {
            _Options = options;
        }
 
        private void ShuffleOptions()
        {
            var ids = _Options.Select(o => o.ID).ToList();
            _Options.Shuffle();
            for (var i = 0; i < _Options.Count; i++)
            {
                var option = _Options[i];
                option.ID = ids[i];
                _Options[i] = option;
            }
        }

#if UNITY_EDITOR
        private void OnOptionsTitleBarGUI(InspectorProperty property)
        {
            if (SirenixEditorGUI.ToolbarButton(FontAwesomeEditorIcons.ShuffleSolid))
            {
                ShuffleOptions();
                property.MarkSerializationRootDirty();
            }
        }   
#endif
    }
}
