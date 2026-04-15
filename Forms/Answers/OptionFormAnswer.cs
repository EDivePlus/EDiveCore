// Author: František Holubec
// Created: 07.04.2026

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace EDIVE.Forms.Answers
{
    [JsonObject(MemberSerialization.OptIn)]
    public class OptionFormAnswer : AFormAnswer
    {
        [JsonProperty("Options")]
        private List<string> _optionIDs;
        public IReadOnlyCollection<string> OptionIDs => _optionIDs;

        public OptionFormAnswer(IEnumerable<string> optionIDs, IEnumerable<IFormAnswerMetadata> metadata = null) : base(metadata)
        {
            _optionIDs = optionIDs?.ToList();
        }
    }
}
