// Author: Michal Petr
// Created: 05.11.2025

using System;

namespace EDIVE.Forms.Questions
{
    [Serializable]
    public class SimpleOptionsQuestion : AOptionsQuestion<SimpleQuestionOption>
    {
        public override string EditorLabel => "Options";
    }
}
