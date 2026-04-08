// Author: Michal Petr
// Created: 05.11.2025

using System;

namespace EDIVE.Forms.Questions
{
    [Serializable]
    public class StringOptionQuestion : AOptionQuestion<QuestionOption<string>>
    {
        public override string EditorLabel => "String Choice";
    }
}
