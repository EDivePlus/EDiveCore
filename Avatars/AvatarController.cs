// Author: František Holubec
// Created: 16.06.2025

using EDIVE.StateHandling.ToggleStates;
using UnityEngine;

namespace EDIVE.Avatars
{
    public class AvatarController : MonoBehaviour
    {
        [SerializeField]
        private ARigFollow _RigFollow;

        [SerializeField]
        private AToggleState _LocalPlayerToggle;

        public ARigFollow RigFollow => _RigFollow;

        public bool IsLocalPlayer
        {
            get => _isLocalPlayer;
            set => SetIsLocalPlayer(value);
        }

        private bool _isLocalPlayer;

        private void SetIsLocalPlayer(bool isLocal)
        {
            _isLocalPlayer = isLocal;
            if (_LocalPlayerToggle)
                _LocalPlayerToggle.State = _isLocalPlayer;
        }
    }
}
