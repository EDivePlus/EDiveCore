// Author: František Holubec
// Created: 08.04.2026

using System;
using System.Collections.Generic;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.Utils;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using ZLinq;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
#endif

namespace EDIVE.Forms.Questions
{
    public interface IQuestionOption
    {
        string ID { get; }
        bool IsCorrect { get; }
    }

    [Serializable]
    public abstract class AQuestionOption : IQuestionOption
    {
        [EnhancedTableColumn(40)]
        [SerializeField]
        private string _ID;

        [PropertyOrder(10)]
        [EnhancedTableColumn(80)]
        [SerializeField]
        private bool _IsCorrect;

        public string ID => _ID;
        public bool IsCorrect => _IsCorrect;

        protected AQuestionOption() { }
        protected AQuestionOption(string id, bool isCorrect) { _ID = id; _IsCorrect = isCorrect; }
        
#if UNITY_EDITOR
        [OnInspectorInit]
        private void OnInspectorInit(InspectorProperty property)
        {
            if (!string.IsNullOrEmpty(_ID) || !property.TryGetParentObject<IEnumerable<IQuestionOption>>(out var parentCollection))
                return;

            GenerateNewID(property, parentCollection);
        }
        
        [UsedImplicitly]
        public void ValidateUniqueID(SelfValidationResult result, InspectorProperty property)
        {
            if (!property.TryGetParentObject(out IEnumerable<IQuestionOption> parentCollection))
                return;
            
            if (parentCollection.AsValueEnumerable().Any(o => o != null && o != this && o.ID == _ID))
            {
                result.AddError($"ID '{_ID}' is not unique.").WithFix(() => GenerateNewID(property, parentCollection));
            }
        }

        private void GenerateNewID(InspectorProperty property, IEnumerable<IQuestionOption> parentCollection)
        {
            _ID =  IdentifierUtility.GenerateAlphaID(this, parentCollection, o => o.ID);
            property.MarkSerializationRootDirty();
        }
#endif
    }
}
