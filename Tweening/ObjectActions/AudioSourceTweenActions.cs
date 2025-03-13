using System;
using DG.Tweening;
using UnityEngine;

namespace EDIVE.Tweening.ObjectActions
{
    [Serializable]
    public class AudioSourceFadeTweenAction : ATweenObjectAction<AudioSource>
    {
        [SerializeField]
        private float _EndValue;
        
        protected override Tween GetTween(AudioSource target) => target.DOFade(_EndValue, _Duration);
    }
    
    [Serializable]
    public class AudioSourcePitchTweenAction : ATweenObjectAction<AudioSource>
    {
        [SerializeField]
        private float _EndValue;
        
        protected override Tween GetTween(AudioSource target) => target.DOPitch(_EndValue, _Duration);
    }
}