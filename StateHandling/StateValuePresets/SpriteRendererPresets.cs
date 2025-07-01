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
        public override void CaptureFrom(SpriteRenderer targetObject) => Value = targetObject.sprite;
    }
    
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class SpriteRendererMaskInteractionPreset: AStateValuePreset<SpriteRenderer, SpriteMaskInteraction>
    {
        public override string Title => "Mask Interaction";
        public override void ApplyTo(SpriteRenderer targetObject) => targetObject.maskInteraction = Value;
        public override void CaptureFrom(SpriteRenderer targetObject) => Value = targetObject.maskInteraction;
    }
    
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class SpriteRendererFlipXPreset: AStateValuePreset<SpriteRenderer, bool>
    {
        public override string Title => "Flip X";
        public override void ApplyTo(SpriteRenderer targetObject) => targetObject.flipX = Value;
        public override void CaptureFrom(SpriteRenderer targetObject) => Value = targetObject.flipX;
    }
    
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class SpriteRendererFlipYPreset: AStateValuePreset<SpriteRenderer, bool>
    {
        public override string Title => "Flip Y";
        public override void ApplyTo(SpriteRenderer targetObject) => targetObject.flipY = Value;
        public override void CaptureFrom(SpriteRenderer targetObject) => Value = targetObject.flipY;
    }
    
    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class SpriteRendererColorPreset: AStateValuePreset<SpriteRenderer, Color>
    {
        public override string Title => "Color";
        public override void ApplyTo(SpriteRenderer targetObject) => targetObject.color = Value;
        public override void CaptureFrom(SpriteRenderer targetObject) => Value = targetObject.color;
    }

    [Serializable, JsonObject(MemberSerialization.OptIn)] 
    public class SpriteRendererAlphaPreset : AStateValuePreset<SpriteRenderer, float>
    {
        public override string Title => "Alpha";
        public override void ApplyTo(SpriteRenderer targetObject) => targetObject.color = targetObject.color.WithA(Value);
        public override void CaptureFrom(SpriteRenderer targetObject) => Value = targetObject.color.a;
    }
}
