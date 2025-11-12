using Newtonsoft.Json;

namespace EDIVE.External.DiscordWebhooks
{
    [JsonObject(MemberSerialization.OptIn)]
    public class EmbedField
    {
        /// <summary>
        /// Name of the field
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Value of the field
        /// </summary>
        [JsonProperty("value")]
        public string Value { get; set; }

        /// <summary>
        /// Whether or not this field should display inline
        /// </summary>
        [JsonProperty("inline")]
        public bool Inline { get; set; }

        public EmbedField(string name, string value, bool inline)
        {
            Name = name;
            Value = value;
            Inline = inline;
        }
    }
}
