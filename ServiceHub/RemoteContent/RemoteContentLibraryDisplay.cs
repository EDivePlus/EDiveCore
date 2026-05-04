// Author: Michal Petr
// Created: 04.05.2026

using System;
using EDIVE.StateHandling.MultiStates;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.ServiceHub.RemoteContent
{
    public class RemoteContentLibraryDisplay : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _NameText;
        
        [SerializeField]
        private TMP_Text _ExtensionText;
        
        [SerializeField]
        private AMultiState _MediaTypeState;
        
        [SerializeField]
        private Button _InteractButton;
        
        private ContentItemSummary _contentInfo;
        
        public event Action<ContentItemSummary> DisplayPressed;

        private void OnEnable()
        {
            if (_InteractButton != null)
                _InteractButton.onClick.AddListener(OnInteractButtonClicked);
        }

        private void OnDisable()
        {
            if (_InteractButton != null)
                _InteractButton.onClick.RemoveListener(OnInteractButtonClicked);
        }

        public void SetContentInfo(ContentItemSummary content)
        {
            if (_NameText != null)
                _NameText.text = content.Name;

            if (_ExtensionText != null)
                _ExtensionText.text = content.Extension;

            if (_MediaTypeState != null)
                _MediaTypeState.SetState(content.MediaTypeKey);
            
            _contentInfo = content;
        }

        private void OnInteractButtonClicked()
        {
            if (_contentInfo != null)
                DisplayPressed?.Invoke(_contentInfo);
        }
    }
}
