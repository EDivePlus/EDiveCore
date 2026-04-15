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
        private Dictionary<string, AFormAnswer> _answers = new();
        
        public IReadOnlyDictionary<string, AFormAnswer> Answers => _answers;

        public FormAnswerBundle() { }
        public FormAnswerBundle(string participantID)
        {
            ParticipantID = participantID;
        }

        public void Set(string questionId, AFormAnswer answer)
        {
            if (answer == null)
            {
                _answers.Remove(questionId);
            }
            else
            {
                _answers[questionId] = answer;
            }
        }

        public bool TryGet(string questionId, out AFormAnswer answer)
        {
            return _answers.TryGetValue(questionId, out answer);
        }
        
        public void Clear()
        {
            _answers.Clear();
        }
    }
}
