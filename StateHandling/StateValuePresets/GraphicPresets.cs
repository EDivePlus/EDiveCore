using System;
using EDIVE.NativeUtils;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.StateHandling.StateValuePresets
{
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class GraphicAlphaPreset : AStateValuePreset<Graphic, float>
    {
        public override string Title => "Alpha";
        public override void ApplyTo(Graphic targetObject) => targetObject.color = targetObject.color.WithA(Value);
    }
    
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class GraphicColorPreset : AStateValuePreset<Graphic, Color>
    {
        public override string Title => "Color";
        public override void ApplyTo(Graphic targetObject) => targetObject.color = Value;
    }
    
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class GraphicMaterialPreset : AStateValuePreset<Graphic, Material>
    {
        public override string Title => "Material";
        public override void ApplyTo(Graphic targetObject) => targetObject.material = Value;
    }
}
