using Newtonsoft.Json;

namespace EDIVE.External.DiscordWebhooks
{
    [JsonObject(MemberSerialization.OptIn)]
    public class DiscordFileAttachment
    {
        public string Filename { get; }
        public byte[] Data { get; }
        public string ContentType { get; }

        public bool IsValid => !string.IsNullOrEmpty(Filename) && Data == null && ContentType != null;

        public DiscordFileAttachment(string filename, byte[] data)
        {
            Filename = filename;
            Data = data;
            ContentType = GetMimeType(filename);
        }
        
        public static string GetMimeType(string filename)
        {
            var ext = System.IO.Path.GetExtension(filename)?.ToLowerInvariant();
            return ext switch
            {
                ".txt" or ".log" => "text/plain",
                ".xml" => "text/xml",
                ".json" => "text/json",
                ".csv" => "text/csv",
                ".zip" => "application/zip",
                ".pdf" => "application/pdf",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" or ".gifv"=> "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                ".wav" => "audio/wav",
                ".mp3" => "audio/mpeg",
                ".ogg" => "audio/ogg",
                ".mp4" => "video/mp4",
                _ => null
            };
        }
    }
}
