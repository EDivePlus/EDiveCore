// Author: František Holubec
// Created: 25.11.2025

using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.External.Discord.Webhooks
{
    public class DiscordWebHookDefinition : ScriptableObject
    {
        [InlineProperty]
        [HideLabel]
        [SerializeField]
        private DiscordWebhook _Webhook;
        public DiscordWebhook Webhook => _Webhook;
    }
}
