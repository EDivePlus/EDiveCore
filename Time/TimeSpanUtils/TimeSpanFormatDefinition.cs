// Author: František Holubec
// Created: 22.07.2025

using System;
using EDIVE.OdinExtensions.Attributes;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Time.TimeSpanUtils
{
    public class TimeSpanFormatDefinition : ATimeSpanFormatDefinition
    {
        [SerializeField]
        [EnhancedValidate("ValidateFormat", ContinuousValidationCheck = true)]
        private string _Format = @"mm\:ss";

        public override string Format(TimeSpan timeSpan)
        {
            try
            { 
                return timeSpan.ToString(_Format);
            }
            catch (Exception)
            {
                return "ERROR";
            }
        }

#if UNITY_EDITOR
        [UsedImplicitly]
        private void ValidateFormat(string format, SelfValidationResult validationResult)
        {
            try
            { 
                _ = TimeSpan.Zero.ToString(format);
            }
            catch (FormatException)
            {
                validationResult.AddError("Invalid format string.");
            }
        }
#endif
    }
}
