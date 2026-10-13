using System;
using DG.Tweening;
using EDIVE.DataStructures.VariableFields;
using EDIVE.Utils.ObjectActions;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.Tweening.ObjectActions
{
    [Serializable]
    public class GraphicFadeTweenAction : ATweenObjectAction<Graphic>
    {
        [SerializeField]
        private float _EndValue;

        protected override Tween GetTween(Graphic target) => target.DOFade(_EndValue, _Duration);
    }
    
    [Serializable]
    public class GraphicColorFieldTweenAction : ATweenObjectAction<Graphic>
    {
        [SerializeField]
        private VariableField<Color> _EndColor = new(Color.white);

        public GraphicColorFieldTweenAction() { }
        public GraphicColorFieldTweenAction(Color endColor) => _EndColor = new VariableField<Color>(endColor);

        protected override Tween GetTween(Graphic target) => target.DOColor(_EndColor.Value, _Duration);
    }

    [Obsolete("Use GraphicColorFieldTweenAction")]
    [Serializable]
    public class GraphicColorTweenAction : ATweenObjectAction<Graphic>, ILegacyObjectAction
    {
        [SerializeField]
        private Color _EndColor;

        protected override Tween GetTween(Graphic target) => target.DOColor(_EndColor, _Duration);

        public IObjectAction Upgrade() => CopyTimingTo(new GraphicColorFieldTweenAction(_EndColor));
    }
    
    [Serializable]
    public class GraphicBlendableColorTweenAction : ATweenObjectAction<Graphic>
    {
        [SerializeField]
        private Color _EndColor;
        
        protected override Tween GetTween(Graphic target) => target.DOBlendableColor(_EndColor, _Duration);
    }
}