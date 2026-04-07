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
        private DiscordWebHookDefinition _DiscordWebhook;
        
        [SerializeField]
        private float _SendCooldown = 5;

        [SerializeField] 
        private int _Padding = 5;
        
        [SerializeField] 
        private int _MaxLogs = 40;

        public bool CanSendReport => Time.unscaledTime - _lastSendTime > _SendCooldown;
        public float TimeUntilNextSend => Mathf.Max(0, _SendCooldown - (Time.unscaledTime - _lastSendTime));

        private float _lastSendTime;
        public event Action ReportSent;

        private readonly Queue<string> _recent = new();
        private readonly List<string> _result = new(); 
        private int _afterRemaining;

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

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (_result.Count >= _MaxLogs) return;

            var isError = type is LogType.Error or LogType.Assert or LogType.Exception;
            var entry = isError
                ? $"[{type}] {logString}\n{stackTrace}"
                : $"[{type}] {logString}";

            if (isError)
            {
                while (_recent.Count > 0 && _result.Count < _MaxLogs)
                    _result.Add(_recent.Dequeue());

                _recent.Clear();

                if (_result.Count < _MaxLogs)
                    _result.Add(entry);

                _afterRemaining = _Padding;
            }
            else if (_afterRemaining > 0)
            {
                _result.Add(entry);
                _afterRemaining--;
            }
            else
            {
                if (_recent.Count >= _Padding)
                    _recent.Dequeue();
                _recent.Enqueue(entry);
            }
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
            if (!_DiscordWebhook.Webhook.IsValid) return;

            var message = new DiscordMessage
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
            };

            var attachments = new List<DiscordFileAttachment>();
            if (_result.Count > 0)
                attachments.Add(new DiscordFileAttachment("Report.txt",
                    Encoding.UTF8.GetBytes(string.Join("\n", _result))));

            _DiscordWebhook.Webhook.SendMessage(message, attachments);

            _result.Clear();
            _recent.Clear();
            _afterRemaining = 0;

            ReportSent?.Invoke();
        }
    }
}
