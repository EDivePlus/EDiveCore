using System;
using EDIVE.StateHandling.MultiStates;
using UnityEngine;
using UnityEngine.Scripting;

namespace EDIVE.StateHandling.StateValuePresets
{
    [Serializable, Preserve] 
    public class ParticleSystemPlayingPresets : AStateValuePreset<ParticleSystem, bool>
    {
        public override string Title => "Playing";
        
        [SerializeField]
        [Tooltip("Play all child ParticleSystems as well")]
        private bool _WithChildren = true;
        
        public override void ApplyTo(ParticleSystem targetObject)
        {
            if (Value)
                targetObject.Play(_WithChildren);
            else
                targetObject.Stop(_WithChildren);
        }
    }
}
