// Author: František Holubec
// Created: 22.07.2025

using System;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace EDIVE.Time.TimeSpanUtils
{
    public class TimeSpanDisplay : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _Text;
        
        [ShowCreateNew]
        [SerializeField]
        private TimeSpanFormatDefinition _Format;

        [PropertySpace]
        [InlineIconButton(FontAwesomeEditorIconType.SquareUpSolid, nameof(UpdateText), "Apply")]
        [SerializeField]
        private UTimeSpan _TimeSpan;
        
        public TimeSpan TimeSpan
        {
            get => _TimeSpan;
            set
            {
                _TimeSpan = value;
                UpdateText();
            }
        }
        
        public void SetTimeSpan(TimeSpan timeSpan)
        {
            TimeSpan = timeSpan;
            UpdateText();
        }

        private void UpdateText()
        {
            if (_Text == null || _Format == null)
                return;
            
            _Text.text = _Format.Format(TimeSpan);
        }
    }
}
