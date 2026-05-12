// Author: František Holubec
// Created: 16.06.2025

using EDIVE.StateHandling.ToggleStates;
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
        
        public ARigFollow RigFollow => _RigFollow;
        
        public void SetLocalPlayer(bool isLocal)
        {
            if(_LocalPlayerToggle)
                _LocalPlayerToggle.SetState(isLocal);
        }
    }
}
