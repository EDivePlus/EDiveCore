using System;
using Newtonsoft.Json;

namespace EDIVE.External.DiscordWebhooks
{
    public class EmbedAuthor
    {
        /// <summary>
        /// Name of author
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Url of author
        /// </summary>
        [JsonProperty("url")]
        public Uri Url { get; set; }

        /// <summary>
        /// Url of author icon(only supports http(s))
        /// </summary>
        [JsonProperty("icon_url")]
        public Uri IconUrl { get; set; }

        /// <summary>
        /// A proxied url of author icon
        /// </summary>
        [JsonProperty("proxy_icon_url")]
        public Uri ProxyIconUrl { get; set; }
    }
}
