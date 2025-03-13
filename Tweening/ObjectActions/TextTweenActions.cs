using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.Tweening.ObjectActions
{
    [Serializable]
    public class TextColorTweenAction : ATweenObjectAction<Text>
    {
        [SerializeField]
        private Color _EndColor;

        protected override Tween GetTween(Text target) => target.DOColor(_EndColor, _Duration);
    }
    
    [Serializable]
    public class TextFadeTweenAction : ATweenObjectAction<Text>
    {
        [SerializeField]
        private float _EndValue;

        protected override Tween GetTween(Text target) => target.DOFade(_EndValue, _Duration);
    }
    
    [Serializable]
    public class TextBlendableColorTweenAction : ATweenObjectAction<Text>
    {
        [SerializeField]
        private Color _EndColor;

        protected override Tween GetTween(Text target) => target.DOBlendableColor(_EndColor, _Duration);
    }
    
    [Serializable]
    public class TextTextTweenAction : ATweenObjectAction<Text>
    {
        [SerializeField]
        private string _EndText;
        
        [SerializeField]
        private bool _RichTextEnabled = true;
        
        [SerializeField]
        private ScrambleMode _ScrambleMode = ScrambleMode.None;
        
        [SerializeField]
        private string _ScrambleChars;

        protected override Tween GetTween(Text target) => target.DOText(_EndText, _Duration, _RichTextEnabled, _ScrambleMode, string.IsNullOrEmpty(_ScrambleChars) ? null : _ScrambleChars);
    }
}