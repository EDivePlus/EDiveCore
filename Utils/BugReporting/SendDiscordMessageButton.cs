// Author: František Holubec
// Created: 20.04.2026

using System;
using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using EDIVE.Core;
using EDIVE.External.Discord.Webhooks;
using EDIVE.StateHandling.ToggleStates;
using EDIVE.Utils.Activations;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.Utils.BugReporting
{
    public class SendDiscordMessageButton : MonoBehaviour
    {
        [SerializeField] 
        private DiscordWebHookDefinition _DiscordWebhook;
        
        [SerializeField] 
        private TMP_Text _TmpText;
        
        [SerializeField] 
        private Text _Text;
        
        [SerializeReference]
        private IActivation _Activation;

        [SerializeField]
        private float _SendCooldown;
            
        [SerializeField]
        private AToggleState _CanSendToggle;
        
        [SerializeField] 
        private Image _CooldownFill;
        
        private Tweener _tween;
        private float _lastSendTime;

        private bool CanSendReport => Time.unscaledTime - _lastSendTime > _SendCooldown;
        private float TimeUntilNextSend => Mathf.Max(0, _SendCooldown - (Time.unscaledTime - _lastSendTime));
        
        private void OnEnable()
        {
            _Activation?.RegisterActivationListener(OnActivated);
            RefreshState();
        }

        private void OnDisable()
        {
            _Activation?.UnregisterActivationListener(OnActivated);
        }

        public void OnActivated()
        {
            TrySendSendMessage();
        }

        private void RefreshState()
        {
            _tween?.Kill();
            
            if (CanSendReport)
            {
                if (_CanSendToggle != null)
                    _CanSendToggle.SetState(true);
                
                if (_CooldownFill != null)
                    _CooldownFill.fillAmount = 0;
            }
            else
            {
                if (_CanSendToggle != null)
                    _CanSendToggle.SetState(false);
                
                if (_CooldownFill != null)
                {
                    _CooldownFill.fillAmount = 1;
                    _tween = _CooldownFill
                        .DOFillAmount(0f, TimeUntilNextSend)
                        .SetEase(Ease.Linear)
                        .OnComplete(() =>
                        {
                            if (_CanSendToggle != null)
                                _CanSendToggle.SetState(true);
                        });
                }
            }
        }
        
        private bool TryGetText(out string text)
        {
            if (_TmpText != null)
            {
                text = _TmpText.text;
                return true;
            }
            
            if (_Text != null)
            {
                text = _Text.text;
                return true;
            }
            
            text = null;
            return false;
        }
        
        public void TrySendSendMessage()
        {
            if (Time.unscaledTime - _lastSendTime < _SendCooldown)
                return;

            _lastSendTime = Time.unscaledTime;
            SendMessage();
        }
        
        [Button]
        private void SendMessage()
        {
            if (!_DiscordWebhook.Webhook.IsValid) 
                return;
            
            if (!TryGetText(out var text) || string.IsNullOrWhiteSpace(text))
                return;

            var message = new DiscordMessage
            {
                Username = "Console Message",
                AvatarUrl = new Uri("https://cdn-icons-png.flaticon.com/512/2933/2933617.png"),
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

            var attachments = new List<DiscordFileAttachment>
            {
                new("Report.txt", Encoding.UTF8.GetBytes(text))
            };

            _DiscordWebhook.Webhook.SendMessage(message, attachments);
        }
    }
}
