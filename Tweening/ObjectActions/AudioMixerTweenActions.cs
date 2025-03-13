using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;

namespace EDIVE.Tweening.ObjectActions
{
    [Serializable]
    public class AudioMixerSetFloatTweenAction : ATweenObjectAction<AudioMixer>
    {
        [SerializeField]
        private string _Name;
        
        [SerializeField]
        private float _Value;

        protected override Tween GetTween(AudioMixer target) => target.DOSetFloat(_Name, _Value, _Duration);
    }
}