// Author: František Holubec
// Created: 31.08.2026

using System;
using EDIVE.NativeUtils;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.StateHandling.StateValuePresets
{
    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public class SceneSkyboxPreset : AStateValuePreset<SceneRenderSettingsProxy, Material>
    {
        public override string Title => "Skybox";
        public override void ApplyTo(SceneRenderSettingsProxy targetObject) => targetObject.Skybox = Value;
        public override void CaptureFrom(SceneRenderSettingsProxy targetObject) => Value = targetObject.Skybox;
    }
}
