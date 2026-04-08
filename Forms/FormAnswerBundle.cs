// Author: Michal Petr
// Created: 30.10.2025

using EDIVE.Forms.Answers;
using EDIVE.Utils.SerializableDictionary;
using Newtonsoft.Json;

namespace EDIVE.Forms
{
    [JsonObject(MemberSerialization.OptIn)]
    public class FormAnswerBundle
    {
        [JsonProperty("ParticipantID")]
        public string ParticipantID { get; set; }
        
        [JsonProperty("Responses")]
        private SerializableDictionary<string, AFormAnswer> Answers { get; } = new();
        
        public void Set(string questionId, AFormAnswer answer)
        {
            Answers[questionId] = answer;
        }

        public bool TryGet(string questionId, out AFormAnswer answer)
        {
            return Answers.TryGetValue(questionId, out answer);
        }
    }
}
