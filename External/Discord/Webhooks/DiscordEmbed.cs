using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.External.Discord.Webhooks
{
    [JsonObject(MemberSerialization.OptIn)]
    public class DiscordEmbed
    {
        /// <summary>
        /// Title of embed
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// Description of embed
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// Url of embed
        /// </summary>
        [JsonProperty("url")]
        public Uri Url { get; set; }

        /// <summary>
        /// Timestamp of embed content
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Color code of embed
        /// </summary>
        public Color Color { get; set; }
        
        /// <summary>
        /// Footer information
        /// </summary>
        [JsonProperty("footer")]
        public EmbedFooter Footer { get; set; }

        /// <summary>
        /// Image information
        /// </summary>
        [JsonProperty("image")]
        public EmbedMedia Image { get; set; }

        /// <summary>
        /// Thumbnail information
        /// </summary>
        [JsonProperty("thumbnail")]
        public EmbedMedia Thumbnail { get; set; }

        /// <summary>
        /// Video information
        /// </summary>
        [JsonProperty("video")]
        public EmbedMedia Video { get; set; }
        
        /// <summary>
        /// Provider information
        /// </summary>
        [JsonProperty("provider")]
        public EmbedProvider Provider { get; set; }
        
        /// <summary>
        /// Author information
        /// </summary>
        [JsonProperty("author")]
        public EmbedAuthor Author { get; set; }
        
        /// <summary>
        /// Fields information
        /// </summary>
        [JsonProperty("fields")]
        public List<EmbedField> Fields { get; set; } = new();
        
        [JsonProperty("timestamp")]
        private string TimestampProperty => Timestamp.Year is < 1000 or > 9999 ? null : Timestamp.ToString(@"yyyy-MM-ddTHH\:mm\:ss.fffffffzzz");
        
        [JsonProperty("color")]
        private int ColorProperty => ToHexRgb(Color);
        
        public bool ShouldSerialize_Author()
        {
            return !(string.IsNullOrEmpty(Author.Name) && Author.Url.IsWellFormedOriginalString() && Author.IconUrl.IsWellFormedOriginalString());
        }

        public bool ShouldSerialize_Image()
        {
            return Image.Url.IsWellFormedOriginalString();
        }

        public bool ShouldSerialize_Thumbnail()
        {
            return Thumbnail.Url.IsWellFormedOriginalString();
        }

        public bool ShouldSerialize_Fields()
        {
            return Fields != null && Fields.Count > 0;
        }

        public bool ShouldSerialize_Footer()
        {
            return !(string.IsNullOrEmpty(Footer.Text) && Footer.IconUrl.IsWellFormedOriginalString());
        }
        
        private int ToHexRgb(Color color)
        {
            var hs = ((byte) (Color.r * 255)).ToString("X2") + ((byte) (Color.g * 255)).ToString("X2") + ((byte) (Color.b * 255)).ToString("X2");
            return int.Parse(hs, System.Globalization.NumberStyles.HexNumber, null);
        }
    }
}
