// Author: František Holubec
// Created: 10.11.2025

using EDIVE.VisualPresets.Switchers;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace EDIVE.Avatars
{
    public class AvatarDisplay : MonoBehaviour
    {
        [SerializeField]
        private AvatarDefinition _CurrentDefinition;
        
        [SerializeField]
        private AAvatarSelector _Selector;
        
        [SerializeField]
        private TMP_Text _IDText;
        
        [SerializeField]
        private VisualSwitcher _Visual;
        
        public AvatarDefinition CurrentDefinition => _CurrentDefinition;

        private void Awake()
        {
            if (_CurrentDefinition == null)
            {
                RefreshDisplay();
            }
        }

        [Button]
        public void SetDefinition(AvatarDefinition definition)
        {
            _CurrentDefinition = definition;
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            if (_IDText != null) 
                _IDText.text = _CurrentDefinition != null ? _CurrentDefinition.UniqueID : "N/A";

            if (_Selector != null) 
                _Selector.SetDefinition(_CurrentDefinition);
            
            if (_CurrentDefinition != null)
                _Visual?.Apply(_CurrentDefinition.Visual);
        }
    }
}
