// Author: Michal Petr
// Created: 05.11.2025

using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.OdinExtensions.Attributes;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace EDIVE.Forms.Questions
{
    [Serializable]
    public abstract class AOptionQuestion : AFormQuestion 
    {
        [SerializeField]
        [MinMaxSlider(0, "@OptionCount", true)]
        [EnhancedValidate("ValidateSelectionLimits")]
        private Vector2Int _SelectionLimits = new(1, 1);
        
        [UsedImplicitly]
        private int OptionCount => BaseOptions.Count();
        public abstract IEnumerable<IQuestionOption> BaseOptions { get; }
        public Vector2Int SelectionLimits => _SelectionLimits;

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
    public abstract class AOptionQuestion<TOption> : AOptionQuestion where TOption : IQuestionOption
    {
        [EnhancedTableList]
        [SerializeField]
        private List<TOption> _Options = new();
        
        public IReadOnlyList<TOption> Options => _Options;
        public override IEnumerable<IQuestionOption> BaseOptions => _Options.Cast<IQuestionOption>();
    }
    
    public interface IQuestionOption
    {
        bool IsCorrect { get; }
    }

    [Serializable]
    public class QuestionOption<TValue> : IQuestionOption
    {
        [SerializeField]
        private TValue _Value;
        
        [EnhancedTableColumn(90)]
        [SerializeField]
        private bool _IsCorrect;

        public bool IsCorrect => _IsCorrect;
        public TValue Value => _Value;
    }
}
