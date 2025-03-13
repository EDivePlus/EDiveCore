using System;
using EDIVE.StateHandling.MultiStates;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace EDIVE.StateHandling.StateValuePresets
{
    [Serializable, Preserve] 
    public class ImageSpritePreset : AStateValuePreset<Image, Sprite>
    {
        public override string Title => "Sprite";
        public override void ApplyTo(Image targetObject) => targetObject.sprite = Value;
    }
    
    [Serializable, Preserve] 
    public class ImageFillAmountPreset : AStateValuePreset<Image, float>
    {
        public override string Title => "Fill Amount";
        public override void ApplyTo(Image targetObject) => targetObject.fillAmount = Value;
    }
}
