// Author: František Holubec
// Created: 15.06.2026

using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace EDIVE.UIElements.ColorPicker
{
    public class ColorChannelLabel : MonoBehaviour
    {
        [SerializeField]
        private ColorChannel _Channel;

        [SerializeField]
        [Required]
        private TMP_Text _Label;

        [SerializeField]
        private string _Prefix = "";

        [SerializeField]
        private float _MinValue = 0f;

        [SerializeField]
        private float _MaxValue = 255f;

        [SerializeField]
        [MinValue(0)]
        private int _Precision = 0;

        public ColorChannel Channel => _Channel;
        
        public void SetValue(float normalized)
        {
            if (!_Label)
                return;

            var value = Mathf.Lerp(_MinValue, _MaxValue, Mathf.Clamp01(normalized));
            _Label.text = _Prefix + (_Precision > 0 ? value.ToString("F" + _Precision) : Mathf.RoundToInt(value).ToString());
        }
    }
}
