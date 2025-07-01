using System;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.StateHandling.StateValuePresets
{
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class ParticleSystemPlayingPresets : AStateValuePreset<ParticleSystem, bool>
    {
        public override string Title => "Playing";
        
        [SerializeField]
        [Tooltip("Play all child ParticleSystems as well")]
        [JsonProperty("WithChildren")]
        private bool _WithChildren = true;
        
        public override void ApplyTo(ParticleSystem targetObject)
        {
            if (Value)
                targetObject.Play(_WithChildren);
            else
                targetObject.Stop(_WithChildren);
        }

        public override void CaptureFrom(ParticleSystem targetObject) => Value = targetObject.isPlaying;
    }
}
