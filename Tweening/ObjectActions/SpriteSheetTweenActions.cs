using System;
using DG.Tweening;

namespace EDIVE.Tweening.ObjectActions
{
    [Serializable]
    public class SpriteSheetTweenActions : ATweenObjectAction<SpriteSheetTweener>
    {
        protected override Tween GetTween(SpriteSheetTweener target)
        {
            if (target.SpriteSheetDefinition == null) return null;

            var sprites = target.SpriteSheetDefinition.Sprites;
            var tween = DOTween.To(() => target.SpriteIndex, x => target.SpriteIndex = x, sprites.Length - 1, _Duration);
            return tween;
        }
    }
}
