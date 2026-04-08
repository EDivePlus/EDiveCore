// Author: František Holubec
// Created: 07.04.2026

using Newtonsoft.Json;

namespace EDIVE.Forms.Answers
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ValueFormAnswer<T> : AFormAnswer
    {
        [JsonProperty("Value")]
        private T _value;

        public T Value
        {
            get => _value;
            set => _value = value;
        }

        public ValueFormAnswer(T value)
        {
            _value = value;
        }
    }
}
