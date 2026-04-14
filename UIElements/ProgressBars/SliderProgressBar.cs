// Author: František Holubec
// Created: 14.04.2026

using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.UIElements.ProgressBars
{
    public class SliderProgressBar : AProgressBar
    {
        [SerializeField]
        private Slider _Slider;
        
        public override float Progress
        {
            get => _Slider != null ? _Slider.value : 0;
            set
            {
                if (_Slider)     
                    _Slider.value = value;
            }
        }
    }
}
