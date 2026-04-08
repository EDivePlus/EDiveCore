// Author: František Holubec
// Created: 08.10.2025

using System;
using EDIVE.Forms.Answers;
using EDIVE.Forms.Questions;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace EDIVE.Forms.Controllers
{
    public class OpenQuestionController : AFormQuestionController<OpenQuestion>
    {
        [Required]
        [SerializeField]
        private TMP_InputField _InputField;
        
        protected override void Initialize(OpenQuestion formQuestion)
        {
            if (_InputField)
            {
                _InputField.text = string.Empty;
                switch (formQuestion.ChangeTrigger)
                {
                    case OpenQuestion.ChangeTriggerType.ValueChanged:
                        _InputField.onValueChanged.AddListener(ConfirmAnswer);
                        break;
                    case OpenQuestion.ChangeTriggerType.EndEdit: 
                        _InputField.onEndEdit.AddListener(ConfirmAnswer);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override void Terminate()
        {
            if (_InputField)
            {
                _InputField.onValueChanged.RemoveListener(ConfirmAnswer);
                _InputField.onEndEdit.RemoveListener(ConfirmAnswer);
            }
        }

        private void ConfirmAnswer(string value)
        {
            SetAnswer(new ValueFormAnswer<string>(value));
        }
    }
}
