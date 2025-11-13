using System;
using Newtonsoft.Json;

namespace EDIVE.External.Discord.Webhooks
{
    [JsonObject(MemberSerialization.OptIn)]
    public class EmbedMedia
    {
        /// <summary>
        /// Source url (only supports http(s))
        /// </summary>
        [JsonProperty("url")]
        public Uri Url { get; set; }

        /// <summary>
        /// Proxy url of the media
        /// </summary>
        [JsonProperty("proxy_url")]
        public Uri ProxyUrl { get; set; }

        /// <summary>
        /// Height of media
        /// </summary>
        [JsonProperty("height")]
        public int Height { get; set; }

        /// <summary>
        /// Width of media
        /// </summary>
        [JsonProperty("width")]
        public int Width { get; set; }

        public EmbedMedia(Uri url)
        {
            Url = url;
        }
    }
}
