using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace EDIVE.External.Discord.Webhooks
{
    [JsonObject(MemberSerialization.OptIn)]
    public class DiscordMessage
    {
        /// <summary>
        /// The message contents (up to 2000 characters)
        /// </summary>
        [JsonProperty("content")]
        public string Content { get; set; }

        /// <summary>
        /// Override the default username of the webhook
        /// </summary>
        [JsonProperty("username")]
        public string Username { get; set; }

        /// <summary>
        /// Override the default avatar of the webhook
        /// </summary>
        [JsonProperty("avatar_url")]
        public Uri AvatarUrl { get; set; }

        /// <summary>
        /// True if this is a TTS message
        /// </summary>
        [JsonProperty("tts")]
        public bool TTS { get; set; }

        /// <summary>
        /// Embedded rich content
        /// </summary>
        [JsonProperty("embeds")]
        public List<DiscordEmbed> Embeds { get; set; } = new();
        
        /// <summary>
        /// Name of thread to create (requires the webhook channel to be a forum channel)
        /// </summary>
        [JsonProperty("thread_name")]
        public string ThreadName { get; set; }
        
        public bool ShouldSerialize_Embed() => Embeds != null && Embeds.Count > 0;
    }
}
