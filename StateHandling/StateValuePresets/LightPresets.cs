// Author: František Holubec
// Created: 31.08.2026

using System;
using EDIVE.OdinExtensions.Attributes;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.StateHandling.StateValuePresets
{
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class LightIntensityPreset : AStateValuePreset<Light, float>
    {
        public override string Title => "Intensity";
        public override void ApplyTo(Light targetObject) => targetObject.intensity = Value;
        public override void CaptureFrom(Light targetObject) => Value = targetObject.intensity;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public class LightColorPreset : AStateValuePreset<Light, Color>
    {
        public override string Title => "Color";
        public override void ApplyTo(Light targetObject)
        {
            targetObject.useColorTemperature = false;
            targetObject.color = Value;
        }

        public override void CaptureFrom(Light targetObject) => Value = targetObject.color;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public class LightFilterAndTemperaturePreset : AStateValuePreset<Light>
    {
        [SerializeField]
        [JsonProperty("Filter")]
        private Color _Filter = new(1f, 0.96f, 0.84f);

        [ColorTemperature]
        [SerializeField]
        [JsonProperty("Temperature")]
        private float _Temperature = 6570;

        public override string Title => "Filter And Temperature";

        public override void ApplyTo(Light targetObject)
        {
            targetObject.useColorTemperature = true;
            targetObject.color = _Filter;
            targetObject.colorTemperature = _Temperature;
        }

        public override void CaptureFrom(Light targetObject)
        {
            _Filter = targetObject.color;
            _Temperature = targetObject.colorTemperature;
        }
    }
}
