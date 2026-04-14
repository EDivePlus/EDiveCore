// Author: František Holubec
// Created: 14.04.2026

using EDIVE.Forms.Questions;
using TMPro;
using UnityEngine;

namespace EDIVE.Forms.Controllers
{
    public class SimpleOptionsQuestionController : AOptionsQuestionController<SimpleOptionsQuestion> 
    {
        [SerializeField]
        private TMP_Text _DescriptionText;

        protected override void Initialize(SimpleOptionsQuestion question)
        {
            base.Initialize(question);
            if (_DescriptionText)
                _DescriptionText.text = question.Description;
        }
    }
}
