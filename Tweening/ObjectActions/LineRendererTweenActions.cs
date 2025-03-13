using System;
using DG.Tweening;
using UnityEngine;

namespace EDIVE.Tweening.ObjectActions
{
    [Serializable]
    public class LineRendererColorTweenAction : ATweenObjectAction<LineRenderer>
    {
        [SerializeField]
        private Color _StartColorA;
        
        [SerializeField]
        private Color _StartColorB;
        
        [SerializeField]
        private Color _EndColorA;
        
        [SerializeField]
        private Color _EndColorB;

        protected override Tween GetTween(LineRenderer target) => 
            target.DOColor(new Color2(_StartColorA, _StartColorB), new Color2(_EndColorA, _EndColorB), _Duration);
    }
    
    [Serializable]
    public class LineRendererWidthTweenAction : ATweenObjectAction<LineRenderer>
    {
        [SerializeField]
        private float _StartWidth;
        
        [SerializeField]
        private float _EndWidth;

        protected override Tween GetTween(LineRenderer target)
        {
            var sequence = DOTween.Sequence();
            sequence.Append(DOTween.To(() => target.startWidth, x => target.startWidth = x, _StartWidth, _Duration));
            sequence.Join(DOTween.To(() => target.endWidth, x => target.endWidth = x, _StartWidth, _Duration));
            return sequence;
        }
    }
}