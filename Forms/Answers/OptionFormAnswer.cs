// Author: František Holubec
// Created: 07.04.2026

using System.Collections.Generic;
using EDIVE.Forms.Questions;

namespace EDIVE.Forms.Answers
{
    public class OptionFormAnswer<TOption> : AFormAnswer where TOption : IQuestionOption
    {
        private readonly List<TOption> _options;
        public IReadOnlyCollection<TOption> Options => _options;

        public OptionFormAnswer(List<TOption> options)
        {
            _options = options;
        }
    }
}
