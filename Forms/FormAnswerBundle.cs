// Author: Michal Petr
// Created: 30.10.2025

using System;
using System.Collections.Generic;
using EDIVE.Forms.Answers;
using Newtonsoft.Json;
using Sirenix.OdinInspector;

namespace EDIVE.Forms
{
    [JsonObject(MemberSerialization.OptIn)]
    [Serializable]
    public class FormAnswerBundle
    {
        [ShowInInspector]
        [JsonProperty("ParticipantID")]
        public string ParticipantID { get; }
        
        [ShowInInspector]
        [JsonProperty("Responses")]
        private Dictionary<string, AFormAnswer> Answers { get; } = new();
        
        public FormAnswerBundle(string participantID)
        {
            ParticipantID = participantID;
        }

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
