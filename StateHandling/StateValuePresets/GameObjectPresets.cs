using System;
using EDIVE.OdinExtensions.Attributes;
using UnityEngine;
using UnityEngine.Scripting;

namespace EDIVE.StateHandling.StateValuePresets
{
    [Serializable, Preserve] 
    public class GameObjectActivePreset : AStateValuePreset<GameObject, bool>
    {
        public override string Title => "Active";
        public override void ApplyTo(GameObject targetObject) => targetObject.SetActive(Value);
    }

    [Serializable, Preserve]
    public class GameObjectLayerPreset : AStateValuePreset<GameObject>
    {
        [SerializeField]
        [LayerField]
        private int _Layer;

        public override string Title => "Layer";
        public override void ApplyTo(GameObject targetObject) => targetObject.layer = _Layer;
    }
}
