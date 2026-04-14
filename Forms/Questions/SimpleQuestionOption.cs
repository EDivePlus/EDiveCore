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
    }
}
