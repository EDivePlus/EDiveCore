// Author: Michal Petr
// Created: 05.11.2025

using System;
using System.Collections.Generic;
using UnityEngine;

namespace EDIVE.Forms.Questions
{
    [Serializable]
    public class SimpleOptionsQuestion : AOptionsQuestion<SimpleQuestionOption>
    {
        public override string EditorLabel => "Options";
        
        [SerializeField]
        private string _Description;
        public string Description => _Description;

        public SimpleOptionsQuestion(string description)
        {
            _Description = description;
        }

        public SimpleOptionsQuestion(string id, string description, List<SimpleQuestionOption> options) : base(id, options)
        {
            _Description = description;
        }
    }
}
