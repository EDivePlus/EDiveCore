// Author: František Holubec
// Created: 07.04.2026

using Newtonsoft.Json;

namespace EDIVE.Forms.Answers
{
    [JsonObject(MemberSerialization.OptIn)]
    public class FormAnswer<T> : AFormAnswer
    {
        [JsonProperty("Value")]
        private T _value;

        public T Value
        {
            get => _value;
            set => _value = value;
        }

        public FormAnswer(T value)
        {
            _value = value;
        }
    }
}
