// Author: František Holubec
// Created: 29.10.2026

using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.ServiceHub.Auth
{
    // Sign-in button of one provider, hidden unless the provider is set up in ServiceHubSettings
    public class LoginProviderButton : MonoBehaviour
    {
        [SerializeField]
        [ValueDropdown(nameof(GetProviderIds))]
        private string _ProviderId;

        [SerializeField]
        [Required]
        private Button _Button;

        public string ProviderId => _ProviderId;

        public event Action<string> Clicked;

        private void OnEnable() => _Button.onClick.AddListener(OnClicked);
        private void OnDisable() => _Button.onClick.RemoveListener(OnClicked);

        private void OnClicked() => Clicked?.Invoke(_ProviderId);

        private static IEnumerable<string> GetProviderIds()
        {
#if UNITY_EDITOR
            return UnityEditor.TypeCache.GetTypesDerivedFrom<ALoginProvider>()
                .Where(t => !t.IsAbstract)
                .Select(t => ((ALoginProvider) Activator.CreateInstance(t)).Id);
#else
            return Enumerable.Empty<string>();
#endif
        }
    }
}
