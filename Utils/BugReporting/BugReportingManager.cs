// Author: František Holubec
// Created: 25.11.2025

using System;
using System.Collections.Generic;
using System.Text;
using EDIVE.Core.Services;
using EDIVE.External.Discord.Webhooks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Utils.BugReporting
{
    public class BugReportingManager : AServiceBehaviour<BugReportingManager>
    {
        [SerializeField]
        private float _SendCooldown = 5;
        
        [SerializeField]
        private DiscordWebhook _DiscordWebhook;
        
        [SerializeField]
        private int _MaxLogs = 20;
        
        [SerializeField]
        private int _MaxErrors = 10;
        
        public bool CanSendReport => Time.unscaledTime - _lastSendTime > _SendCooldown;
        public float TimeUntilNextSend => Mathf.Max(0, _SendCooldown - (Time.unscaledTime - _lastSendTime));
        
        private float _lastSendTime;
        public event Action ReportSent;
        
        private readonly Queue<string> _logs = new();
        private readonly Queue<string> _errors = new();

        protected override void OnEnable()
        {
            base.OnEnable();
            Application.logMessageReceived += HandleLog;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Application.logMessageReceived -= HandleLog;
        }
        
        private void HandleLog(string condition, string stackTrace, LogType type)
        {
            var entry = $"[{type}] {condition}";

            AddToQueue(_logs, entry, _MaxLogs);
            if (type is LogType.Error or LogType.Assert or LogType.Exception)
            {
                AddToQueue(_errors, entry + "\n" + stackTrace, _MaxErrors);
            }
        }

        private static void AddToQueue(Queue<string> queue, string entry, int limit)
        {
            if (queue.Count >= limit)
                queue.Dequeue();

            queue.Enqueue(entry);
        }

        public void TrySendReport()
        {
            if (Time.unscaledTime - _lastSendTime < _SendCooldown)
                return;

            _lastSendTime = Time.unscaledTime;
            SendReport();
        }
        
        [Button]
        private void SendReport()
        {
            if (!_DiscordWebhook.IsValid) 
                return;
            
            _DiscordWebhook.SendMessage(new DiscordMessage
            {
                Username = "Reporter",
                AvatarUrl = new Uri("https://cdn-icons-png.flaticon.com/512/1320/1320452.png"),
                Embeds = new List<DiscordEmbed>
                {
                    new()
                    {
                        Color = new Color(0.6f, 0f, 0f),
                        Timestamp = DateTime.UtcNow,
                        Fields = new List<EmbedField>
                        {
                            new("App", Application.productName, true),
                            new("Version", Application.version, true),
                            new("Device Model", SystemInfo.deviceModel, true),
                            new("Device Name", SystemInfo.deviceName, true),
                        }
                    }
                }
            }, new List<DiscordFileAttachment>
            {
                new("Logs.txt", Encoding.UTF8.GetBytes(string.Join("\n", _logs))),
                new("Errors.txt", Encoding.UTF8.GetBytes(string.Join("\n", _errors)))
            });
            ReportSent?.Invoke();
        }
    }
}
