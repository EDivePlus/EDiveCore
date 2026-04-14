// Author: František Holubec
// Created: 14.04.2026

using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.UIElements.ProgressBars
{
    public class ImageFillProgressBar : AProgressBar
    {
        [SerializeField]
        private Image _Image;
        
        [SerializeField]
        private Vector2 _FillRange = new(0, 1);
        
        public override float Progress
        {
            get => _Image == null ? 0f : Mathf.InverseLerp(_FillRange.x, _FillRange.y, _Image.fillAmount);
            set
            {
                if (_Image)
                    _Image.fillAmount = Mathf.Lerp(_FillRange.x, _FillRange.y, value);
            }
        }
    }
}
