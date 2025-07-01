using System;
using EDIVE.NativeUtils;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.StateHandling.StateValuePresets
{
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class SpriteRendererSpritePreset : AStateValuePreset<SpriteRenderer, Sprite>
    {
        public override string Title => "Sprite";
        public override void ApplyTo(SpriteRenderer targetObject) => targetObject.sprite = Value;
    }
    
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class SpriteRendererMaskInteractionPreset: AStateValuePreset<SpriteRenderer, SpriteMaskInteraction>
    {
        public override string Title => "Mask Interaction";
        public override void ApplyTo(SpriteRenderer targetObject) => targetObject.maskInteraction = Value;
    }
    
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class SpriteRendererFlipXPreset: AStateValuePreset<SpriteRenderer, bool>
    {
        public override string Title => "Flip X";
        public override void ApplyTo(SpriteRenderer targetObject) => targetObject.flipX = Value;
    }
    
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class SpriteRendererFlipYPreset: AStateValuePreset<SpriteRenderer, bool>
    {
        public override string Title => "Flip Y";
        public override void ApplyTo(SpriteRenderer targetObject) => targetObject.flipY = Value;
    }
    
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class SpriteRendererColorPreset: AStateValuePreset<SpriteRenderer, Color>
    {
        public override string Title => "Color";
        public override void ApplyTo(SpriteRenderer targetObject) => targetObject.color = Value;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class SpriteRendererAlphaPreset : AStateValuePreset<SpriteRenderer, float>
    {
        public override string Title => "Alpha";
        public override void ApplyTo(SpriteRenderer targetObject) => targetObject.color = targetObject.color.WithA(Value);
    }
}
