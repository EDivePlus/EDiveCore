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
        
        protected override void Initialize()
        {
            base.Initialize();
            if (_InputField)
            {
                _InputField.text = string.Empty;
                switch (Question.ChangeTrigger)
                {
                    case OpenQuestion.ChangeTriggerType.ValueChanged:
                        _InputField.onValueChanged.AddListener(OnConfirmAnswer);
                        break;
                    case OpenQuestion.ChangeTriggerType.EndEdit: 
                        _InputField.onEndEdit.AddListener(OnConfirmAnswer);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public override void Terminate()
        {
            base.Terminate();
            if (_InputField)
            {
                _InputField.onValueChanged.RemoveListener(OnConfirmAnswer);
                _InputField.onEndEdit.RemoveListener(OnConfirmAnswer);
            }
        }

        public override void SetAnswer(AFormAnswer answer)
        {
            if (answer is not ValueFormAnswer<string> stringAnswer) 
                return;
            
            _InputField.text = stringAnswer.Value;
        }

        private void OnConfirmAnswer(string value)
        {
            SubmitAnswer(new ValueFormAnswer<string>(value));
        }
    }
}
