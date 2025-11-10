// Author: František Holubec
// Created: 10.11.2025

using UnityEngine;

namespace EDIVE.Avatars
{
    public abstract class AAvatarSelector : MonoBehaviour
    {
        [SerializeField]
        private AvatarDefinition _Definition;
        
        public AvatarDefinition Definition => _Definition;
        
        public virtual void SetDefinition(AvatarDefinition definition)
        {
            _Definition = definition;
        }
    }
}
