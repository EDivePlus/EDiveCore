// Author: František Holubec
// Created: 13.02.2026

using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using EDIVE.OdinExtensions.Attributes;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Time.DateTimeUtils
{
    public class DateTimeFormatDefinition : ADateTimeFormatDefinition
    {
        [SerializeField]
        [EnhancedValidate("ValidateFormat", ContinuousValidationCheck = true)]
        private string _Format = @"dd/MM/yyyy";

        [SerializeField]
        private CultureType _CultureType;

        [ShowIf(nameof(_CultureType), CultureType.Custom)]
        [EnhancedValueDropdown("GetAvailableCultures")]
        [SerializeField]
        private string _CultureName = "";

        [SerializeField]
        private TimeZoneType _TimeZoneType;

        [ShowIf(nameof(_TimeZoneType), TimeZoneType.Custom)]
        [EnhancedValueDropdown("GetAvailableTimeZones")]
        [SerializeField]
        private string _TimeZoneId = "";

        public override string Format(DateTime dateTime)
        {
            try
            {
                var targetTimeZone = _TimeZoneType switch
                {
                    TimeZoneType.System => TimeZoneInfo.Local,
                    TimeZoneType.Custom when !string.IsNullOrEmpty(_TimeZoneId) => TimeZoneInfo.FindSystemTimeZoneById(_TimeZoneId),
                    _ => TimeZoneInfo.Local
                };
            
                dateTime = dateTime.Kind switch
                {
                    DateTimeKind.Utc => TimeZoneInfo.ConvertTimeFromUtc(dateTime, targetTimeZone),
                    DateTimeKind.Local => TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local, targetTimeZone),
                    _ => dateTime
                };
                
                var culture = _CultureType switch
                {
                    CultureType.System => CultureInfo.CurrentCulture,
                    CultureType.Invariant => CultureInfo.InvariantCulture,
                    CultureType.Custom when !string.IsNullOrEmpty(_CultureName) => CultureInfo.GetCultureInfo(_CultureName),
                    _ => CultureInfo.InvariantCulture
                };
                
                return dateTime.ToString(_Format, culture);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return "ERROR";
            }
        }
        
#if UNITY_EDITOR
        [UsedImplicitly]
        private void ValidateFormat(string format, SelfValidationResult validationResult)
        {
            try
            { 
                _ = DateTime.UnixEpoch.ToString(format);
            }
            catch (FormatException)
            {
                validationResult.AddError("Invalid format string.");
            }
        }
        
        [UsedImplicitly]
        private static IEnumerable GetAvailableTimeZones()
        {
            return TimeZoneInfo.GetSystemTimeZones()
                .OrderBy(t => t.Id != "UTC")
                .ThenBy(t => t.BaseUtcOffset)
                .Select(t => new ValueDropdownItem<string>($"{t.DisplayName} ({t.Id})", t.Id));
        }
        
        [UsedImplicitly]
        private static IEnumerable GetAvailableCultures()
        {
            return CultureInfo.GetCultures(CultureTypes.SpecificCultures)
                .OrderBy(c => c.EnglishName)
                .Select(c => new ValueDropdownItem<string>($"{c.EnglishName}", c.Name));
        }
#endif

        public enum CultureType
        {
            System,
            Invariant,
            Custom
        }
        
        public enum TimeZoneType
        {
            System,
            Custom
        }
    }
}
