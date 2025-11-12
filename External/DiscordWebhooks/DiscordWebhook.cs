using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Networking;

namespace EDIVE.External.DiscordWebhooks
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
        public static DiscordMessage CreateTestMessage()
        {
            return new DiscordMessage
            {
                Username = "Main UserName",
                AvatarUrl = new Uri("https://upload.wikimedia.org/wikipedia/commons/b/bc/Unknown_person.jpg"),
                Content = "TEST MESSAGE FROM UNITY - Content *Italic* **bold** __underline__ ~~strikeout~~ [hyperlink](https://google.com) `code`",
                Embeds = new List<DiscordEmbed>
                {
                    new()
                    {
                        Author = new EmbedAuthor
                        {
                            Name = "Author name (with url link)",
                            Url = new Uri("https://www.youtube.com/watch?v=FavUpD_IjVY&ab_channel=cyriak"),
                            IconUrl = new Uri("https://ryanharveyauthor.files.wordpress.com/2018/09/clydeeghost.png")
                        },
                        Color = Color.green,
                        Title = "Embed Title (with url link)",
                        Url = new Uri("https://www.youtube.com/watch?v=oHg5SJYRHA0&ab_channel=cotter548"),
                        Description = "Embed Description",
                        Image = new EmbedMedia(new Uri("https://mixed-news.com/en/wp-content/uploads/2022/10/Into-the-Radius-Artwork-3.png")),
                        Thumbnail = new EmbedMedia(new Uri("https://static.wikia.nocookie.net/satisfactory_gamepedia_en/images/7/79/Overclocking_Icon.png/revision/latest?cb=20200301112223")),
                        Footer = new EmbedFooter
                        {
                            Text = "Footer Text",
                            IconUrl = new Uri("https://media.tenor.com/images/f4eb1739c0451110c0285920477a5a8f/tenor.png")
                        },
                        Fields = new List<EmbedField>
                        {
                            new("Field 0", "Value 0", false),
                            new("Field 1", "Value 1", true),
                            new("Field 2", "Value 2", true),
                            new("Field 3", "Value 3", true)
                        }
                    }
                }
            };
        }

        [Button]
        public void SendTestMessage()
        {
            SendMessage(CreateTestMessage(), new List<DiscordFileAttachment>
            {
                new("TextFile1.txt", Encoding.UTF8.GetBytes("data data data data data data data data data data data data data data")),
                new("TextFile2.txt", Encoding.UTF8.GetBytes("data data data data data data data data data data data data data data")),
                new("XmlFile.xml", Encoding.UTF8.GetBytes("<?xml version=\"1.0\" encoding=\"UTF-8\"?><metadata><groupId>com.google.games</groupId></metadata>"))
            });
        }
#endif
    }
}
