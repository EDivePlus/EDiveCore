// Author: František Holubec
// Created: 08.04.2026

using System;
using UnityEngine;

namespace EDIVE.Forms.Questions
{
    [Serializable]
    public class SimpleQuestionOption : AQuestionOption
    {
        [SerializeField]
        private string _Text;
        public string Text => _Text;

        public SimpleQuestionOption() { }
        public SimpleQuestionOption(string id, string text, bool isCorrect) : base(id, isCorrect) { _Text = text; }
    }
}
