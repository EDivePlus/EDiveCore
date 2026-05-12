// Author: Michal Petr
// Created: 04.05.2026

using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.StateHandling.MultiStates;
using EDIVE.Time.DateTimeUtils;
using EDIVE.Utils.Activations;
using TMPro;
using UnityEngine;

namespace EDIVE.ServiceHub.RemoteContent.UI
{
    public class RemoteContentInfoDisplay : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _NameText;
        
        [SerializeField]
        private TMP_Text _MediaTypeText;
        
        [SerializeField]
        private TMP_Text _ExtensionText;
        
        [SerializeField]
        private DateTimeDisplay _CreatedAtDisplay;
        
        [SerializeField]
        private DateTimeDisplay _UpdatedAtDisplay;

        [SerializeField]
        private ServiceHubUserDisplay _OwnerDisplay;
        
        [SerializeField]
        private AMultiState _MediaTypeState;
        
        [SerializeReference]
        private IActivation _SpawnActivation;
        
        private ContentItemInfo _contentInfo;

        private void OnEnable()
        {
            _SpawnActivation?.RegisterActivationListener(OnSpawnActivated);
            UpdateDisplay();
        }

        private void OnDisable()
        {
            _SpawnActivation?.UnregisterActivationListener(OnSpawnActivated);
        }

        public void ApplyContentInfo(ContentItemInfo content)
        {
            _contentInfo = content;
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (_contentInfo == null)
                return;
            
            if (_NameText != null)
                _NameText.text = _contentInfo.Name;

            if (_MediaTypeText != null)
                _MediaTypeText.text = _contentInfo.MediaTypeKey;
            
            if (_ExtensionText != null)
                _ExtensionText.text = _contentInfo.Extension;
            
            if (_CreatedAtDisplay != null)
                _CreatedAtDisplay.SetDateTime(_contentInfo.CreatedAt);
            
            if (_UpdatedAtDisplay != null)
                _UpdatedAtDisplay.SetDateTime(_contentInfo.UpdatedAt);

            if (_MediaTypeState != null)
                _MediaTypeState.SetState(_contentInfo.MediaTypeKey);
            
            if (_OwnerDisplay != null)
                _OwnerDisplay.SetUserInfo(_contentInfo.Owner);
        }

        private void OnSpawnActivated()
        {
            if (_contentInfo != null)
                AppCore.Services.Get<RemoteContentManager>().SpawnHandlerAsync(_contentInfo).Forget();
        }
    }
}
