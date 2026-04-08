// Author: František Holubec
// Created: 07.04.2026

using System.Collections.Generic;
using EDIVE.Forms.Questions;

namespace EDIVE.Forms.Answers
{
    public class OptionFormAnswer : AFormAnswer
    {
        private readonly List<IQuestionOption> _options;
        public IReadOnlyCollection<IQuestionOption> Options => _options;

        public OptionFormAnswer(List<IQuestionOption> options)
        {
            _options = options;
        }
    }
}
