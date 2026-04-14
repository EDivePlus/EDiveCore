// Author: Michal Petr
// Created: 30.10.2025

using System.Collections.Generic;
using Newtonsoft.Json;

namespace EDIVE.Forms.Answers
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class AFormAnswer
    {
        [JsonProperty("Meta")]
        public List<IFormAnswerMetadata> Metadata { get; } = new();

        public void AddMetadata(IFormAnswerMetadata metadata)
        {
            Metadata.Add(metadata);
        }

        protected AFormAnswer(IEnumerable<IFormAnswerMetadata> metadata = null)
        { 
            if (metadata != null) 
                Metadata.AddRange(metadata);
        }
    }

    public interface IFormAnswerMetadata {}
}
