using System;
using Newtonsoft.Json;

namespace EDIVE.External.DiscordWebhooks
{
    [JsonObject(MemberSerialization.OptIn)]
    public class EmbedProvider
    {
        /// <summary>
        /// Name of provider
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Url of provider
        /// </summary>
        [JsonProperty(PropertyName = "url")]
        public Uri Url { get; set; }
    }
}
