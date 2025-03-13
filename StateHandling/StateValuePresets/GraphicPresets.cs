using EDIVE.NativeUtils;
using EDIVE.StateHandling.MultiStates;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace EDIVE.StateHandling.StateValuePresets
{
    [Preserve]
    public class GraphicAlphaPreset : AStateValuePreset<Graphic, float>
    {
        public override string Title => "Alpha";
        public override void ApplyTo(Graphic targetObject) => targetObject.color = targetObject.color.WithA(Value);
    }
    
    [Preserve]
    public class GraphicColorPreset : AStateValuePreset<Graphic, Color>
    {
        public override string Title => "Color";
        public override void ApplyTo(Graphic targetObject) => targetObject.color = Value;
    }
    
    [Preserve]
    public class GraphicMaterialPreset : AStateValuePreset<Graphic, Material>
    {
        public override string Title => "Material";
        public override void ApplyTo(Graphic targetObject) => targetObject.material = Value;
    }
}
