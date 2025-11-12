using System;
using Newtonsoft.Json;

namespace EDIVE.External.DiscordWebhooks
{
    [JsonObject(MemberSerialization.OptIn)]
    public class EmbedFooter
    {
        /// <summary>
        /// Footer text
        /// </summary>
        [JsonProperty("text")]
        public string Text { get; set; }

        /// <summary>
        /// Url of footer icon (only supports http(s))
        /// </summary>
        [JsonProperty("icon_url")]
        public Uri IconUrl { get; set; }

        /// <summary>
        /// A proxied url of footer icon
        /// </summary>
        [JsonProperty("proxy_icon_url")]
        public Uri ProxyIconUrl { get; set; }
    }
}
