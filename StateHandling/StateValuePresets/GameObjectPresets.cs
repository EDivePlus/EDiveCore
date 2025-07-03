using System;
using EDIVE.OdinExtensions.Attributes;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace EDIVE.StateHandling.StateValuePresets
{
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class GameObjectActivePreset : AStateValuePreset<GameObject, bool>
    {
        public override string Title => "Active";
        public override void ApplyTo(GameObject targetObject) => targetObject.SetActive(Value);
        public override void CaptureFrom(GameObject targetObject) => Value = targetObject.activeSelf;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public class GameObjectLayerPreset : AStateValuePreset<GameObject>
    {
        [FormerlySerializedAs("_Layer")]
        [LayerField]
        [LabelText("$Title")]
        [SerializeField]
        [JsonProperty("Value")]
        private int _Value;

        [SerializeField]
        [JsonProperty("IncludeChildren")]
        private bool _IncludeChildren;

        public override string Title => "Layer";

        public override void ApplyTo(GameObject targetObject)
        {
            targetObject.layer = _Value;

            if (_IncludeChildren)
            {
                foreach (var child in targetObject.GetComponentsInChildren<Transform>(true))
                {
                    child.gameObject.layer = _Value;
                }
            }
        }

        public override void CaptureFrom(GameObject targetObject) => _Value = targetObject.layer;
    }
}
