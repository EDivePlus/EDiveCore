// Author: František Holubec
// Created: 13.02.2026

using System;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace EDIVE.Time.DateTimeUtils
{
    public class DateTimeDisplay : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _Text;
        
        [ShowCreateNew]
        [SerializeField]
        private ADateTimeFormatDefinition _Format;

        [PropertySpace]
        [InlineIconButton(FontAwesomeEditorIconType.SquareUpSolid, nameof(UpdateText), "Apply")]
        [SerializeField]
        private UDateTime _DateTime;
        
        public DateTime DateTime
        {
            get => _DateTime;
            set => SetDateTime(value);
        }
        
        public void SetDateTime(DateTime dateTime)
        {
            _DateTime = dateTime;
            UpdateText();
        }

        private void UpdateText()
        {
            if (_Text == null || _Format == null)
                return;
            
            _Text.text = _Format.Format(DateTime);
        }
    }
}
