using System;
using EDIVE.DataStructures.VariableFields;
using EDIVE.NativeUtils;
using EDIVE.Utils.ObjectActions;
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
        public override void CaptureFrom(Graphic targetObject) => Value = targetObject.color.a;
    }
    
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class GraphicColorFieldPreset : AStateValuePreset<Graphic, VariableField<Color>>
    {
        public GraphicColorFieldPreset() : base(new VariableField<Color>(Color.white)) { }
        public GraphicColorFieldPreset(Color color) : base(new VariableField<Color>(color)) { }

        public override string Title => "Color";
        public override void ApplyTo(Graphic targetObject) => targetObject.color = Value.Value;
        public override void CaptureFrom(Graphic targetObject) => Value.Value = targetObject.color;
    }

    [Obsolete("Use GraphicColorFieldPreset")]
    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public class GraphicColorPreset : AStateValuePreset<Graphic, Color>, ILegacyObjectAction
    {
        public override string Title => "Color (Legacy)";
        public override void ApplyTo(Graphic targetObject) => targetObject.color = Value;
        public override void CaptureFrom(Graphic targetObject) => Value = targetObject.color;

        public IObjectAction Upgrade() => new GraphicColorFieldPreset(Value);
    }
    
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class GraphicMaterialPreset : AStateValuePreset<Graphic, Material>
    {
        public override string Title => "Material";
        public override void ApplyTo(Graphic targetObject) => targetObject.material = Value;
        public override void CaptureFrom(Graphic targetObject) => Value = targetObject.material;
    }
}
