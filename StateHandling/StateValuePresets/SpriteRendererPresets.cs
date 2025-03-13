using System;
using EDIVE.NativeUtils;
using EDIVE.StateHandling.MultiStates;
using UnityEngine;
using UnityEngine.Scripting;

namespace EDIVE.StateHandling.StateValuePresets
{
    [Serializable, Preserve] 
    public class SpriteRendererSpritePreset : AStateValuePreset<SpriteRenderer, Sprite>
    {
        public override string Title => "Sprite";
        public override void ApplyTo(SpriteRenderer targetObject) => targetObject.sprite = Value;
    }
    
    [Serializable, Preserve] 
    public class SpriteRendererMaskInteractionPreset: AStateValuePreset<SpriteRenderer, SpriteMaskInteraction>
    {
        public override string Title => "Mask Interaction";
        public override void ApplyTo(SpriteRenderer targetObject) => targetObject.maskInteraction = Value;
    }
    
    [Serializable, Preserve] 
    public class SpriteRendererFlipXPreset: AStateValuePreset<SpriteRenderer, bool>
    {
        public override string Title => "Flip X";
        public override void ApplyTo(SpriteRenderer targetObject) => targetObject.flipX = Value;
    }
    
    [Serializable, Preserve] 
    public class SpriteRendererFlipYPreset: AStateValuePreset<SpriteRenderer, bool>
    {
        public override string Title => "Flip Y";
        public override void ApplyTo(SpriteRenderer targetObject) => targetObject.flipY = Value;
    }
    
    [Serializable, Preserve] 
    public class SpriteRendererColorPreset: AStateValuePreset<SpriteRenderer, Color>
    {
        public override string Title => "Color";
        public override void ApplyTo(SpriteRenderer targetObject) => targetObject.color = Value;
    }

    [Serializable, Preserve] 
    public class SpriteRendererAlphaPreset : AStateValuePreset<SpriteRenderer, float>
    {
        public override string Title => "Alpha";
        public override void ApplyTo(SpriteRenderer targetObject) => targetObject.color = targetObject.color.WithA(Value);
    }
}
