using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Networking;

namespace EDIVE.External.Discord.Webhooks
{
    public class DiscordWebhook : ScriptableObject
    {
        [SerializeField]
        [Required]
        private string _WebHook;

        [Button]
        public void SendMessage(DiscordMessage message, List<DiscordFileAttachment> textFiles = null)
        {
            SendMessageAsync(message, textFiles).Forget();
        }
        
        public async UniTask<(UnityWebRequest.Result, string)> SendMessageAsync(DiscordMessage message, List<DiscordFileAttachment> fileAttachments = null)
        {
            var settings = new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore
            };
            var json = JsonConvert.SerializeObject(message, Formatting.Indented, settings);
            Debug.Log(json);

            var formData = new List<IMultipartFormSection>
            {
                new MultipartFormDataSection("payload_json", json, Encoding.UTF8, "application/json")
            };
            if (fileAttachments != null)
            {
                for (var i = 0; i < fileAttachments.Count; i++)
                {
                    var attachment = fileAttachments[i];
                    if (!attachment.IsValid)
                        continue;
                    
                    var fieldName = $"files[{i}]";
                    formData.Add(new MultipartFormFileSection(fieldName, attachment.Data, attachment.Filename, attachment.ContentType));
                }
            }

            using var webRequest = UnityWebRequest.Post(_WebHook, formData);
            await webRequest.SendWebRequest();
            
            return (webRequest.result, webRequest.downloadHandler?.text);
        }
        

#if UNITY_EDITOR
        [Button]
        public void SendTestMessage()
        {
            SendMessage(new DiscordMessage
            {
                Username = "Unknown User",
                AvatarUrl = new Uri("https://upload.wikimedia.org/wikipedia/commons/b/bc/Unknown_person.jpg"),
                Content = "This is a test message sent from Unity project\n*Italic* **bold** __underline__ ~~strikeout~~ [hyperlink](https://google.com) `code`",
                Embeds = new List<DiscordEmbed>
                {
                    new()
                    {
                        Author = new EmbedAuthor
                        {
                            Name = "Unknown Author",
                            Url = new Uri("https://en.wikipedia.org/wiki/John_Doe"),
                            IconUrl = new Uri("https://upload.wikimedia.org/wikipedia/commons/b/bc/Unknown_person.jpg")
                        },
                        Color = Color.green,
                        Title = "Embed",
                        Url = new Uri("https://en.wikipedia.org/wiki/Embedded"),
                        Description = "Embed Description",
                        Timestamp = DateTime.UnixEpoch,
                        Image = new EmbedMedia(new Uri("https://t3.ftcdn.net/jpg/01/10/25/70/360_F_110257004_PLp6DNqDdup6AyThxyraiAdgTgwVrle7.jpg")),
                        Thumbnail = new EmbedMedia(new Uri("https://static.thenounproject.com/png/3022241-200.png")),
                        Footer = new EmbedFooter
                        {
                            Text = "Footer",
                            IconUrl = new Uri("https://static.thenounproject.com/png/1364707-200.png")
                        },
                        Fields = new List<EmbedField>
                        {
                            new("Identifier", "#0123", false),
                            new("Location", "Czechia", true),
                            new("Device", "Oculus Quest 3Pro", true),
                        }
                    }
                }
            }, new List<DiscordFileAttachment>
            {
                new("TextAttachment.txt", Encoding.UTF8.GetBytes("This is special text attachment"))
            });
        }
#endif
    }
}
