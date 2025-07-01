using System;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.StateHandling.StateValuePresets
{
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class ImageSpritePreset : AStateValuePreset<Image, Sprite>
    {
        public override string Title => "Sprite";
        public override void ApplyTo(Image targetObject) => targetObject.sprite = Value;
    }
    
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class ImageFillAmountPreset : AStateValuePreset<Image, float>
    {
        public override string Title => "Fill Amount";
        public override void ApplyTo(Image targetObject) => targetObject.fillAmount = Value;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public class ImageFillClockwisePreset : AStateValuePreset<Image, bool>
    {
        public override string Title => "Fill Clockwise";
        public override void ApplyTo(Image targetObject) => targetObject.fillClockwise = Value;
    }
}
