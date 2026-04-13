// Author: František Holubec
// Created: 13.04.2026

using EDIVE.Forms.Questions;
using TMPro;
using UnityEngine;

namespace EDIVE.Forms.Controllers
{
    public class SimpleQuestionOptionDisplay : AQuestionOptionDisplay<SimpleQuestionOption>
    {
        [SerializeField]
        private TMP_Text _MainText;

        protected override void Initialize(SimpleQuestionOption option)
        {
            if (_MainText)
                _MainText.text = option.Text;
        }

        public override void Terminate() { }
    }
}
