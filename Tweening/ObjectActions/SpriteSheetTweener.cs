using EDIVE.DataStructures;
using EDIVE.NativeUtils;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.Tweening.ObjectActions
{
    public class SpriteSheetTweener : MonoBehaviour
    {
        [SerializeField]
        private Image _Image;
        public Image Image => _Image;

        [SerializeField]
        private SpriteRenderer _SpriteRenderer;
        public SpriteRenderer SpriteRenderer => _SpriteRenderer;

        [SerializeField]
        private SpriteSheetDefinition _SpriteSheetDefinition;
        public SpriteSheetDefinition SpriteSheetDefinition { get => _SpriteSheetDefinition; set => _SpriteSheetDefinition = value; }

        private int _spriteIndex;
        public int SpriteIndex { get => _spriteIndex; set => SetSpriteIndex(value); }

        public void SetSpriteIndex(int index)
        {
            if (_SpriteSheetDefinition == null) return;

            var sprites = _SpriteSheetDefinition.Sprites;
            if (sprites == null || sprites.Length == 0) return;

            _spriteIndex = index.PositiveModulo(sprites.Length);

            if (_Image) _Image.sprite = sprites[_spriteIndex];
            if (_SpriteRenderer) _SpriteRenderer.sprite = sprites[_spriteIndex];
        }
    }
}
