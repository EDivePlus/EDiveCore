// Author: František Holubec
// Created: 16.06.2025

using EDIVE.StateHandling.ToggleStates;
using EDIVE.VisualPresets.Presets;
using EDIVE.VisualPresets.Switchers;
using PurrNet;
using UnityEngine;

namespace EDIVE.Avatars
{
    public class AvatarController : NetworkBehaviour
    {
        [SerializeField]
        private ARigFollow _RigFollow;

        [SerializeField]
        private AToggleState _LocalPlayerToggle;
        
        [SerializeField]
        private VisualSwitcher _AvatarSwitcher;
        
        public ARigFollow RigFollow => _RigFollow;
        
        public void SetLocalPlayer(bool isLocal)
        {
            if(_LocalPlayerToggle)
                _LocalPlayerToggle.SetState(isLocal);
        }
        
        public void ApplyCustomizationPreset(VisualPreset preset)
        {
            if (_AvatarSwitcher != null && preset != null) 
                _AvatarSwitcher.Apply(preset);
        }
    }
}
