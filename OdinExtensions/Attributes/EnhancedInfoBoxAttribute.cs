// Author: František Holubec
// Created: 16.09.2026

using System;
using System.Diagnostics;
using Sirenix.OdinInspector;

namespace EDIVE.OdinExtensions.Attributes
{
    [DontApplyToListElements]
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    [Conditional("UNITY_EDITOR")]
    public sealed class EnhancedInfoBoxAttribute : Attribute
    {
        public string Message;
        public string Details;
        public InfoMessageType InfoMessageType;

        public EditorIconsBundle? Bundle;
        public string IconName;
        public EditorIconTextureType IconType = EditorIconTextureType.Highlighted;

        [ColorResolver]
        public string IconColor;

        public string ShowIf;
        public bool GUIAlwaysEnabled;
        public bool Expanded;

        public string ButtonAction;
        public string ButtonLabel;
        public string ButtonTooltip;
        public string ButtonIconName;

        public bool HasButtonIconBundle => _buttonIconBundle.HasValue;
        private EditorIconsBundle? _buttonIconBundle;
        public EditorIconsBundle ButtonIconBundle
        {
            get => _buttonIconBundle ?? default;
            set => _buttonIconBundle = value;
        }

        private FontAwesomeEditorIconType _buttonIcon;
        public FontAwesomeEditorIconType ButtonIcon
        {
            get => _buttonIcon;
            set
            {
                _buttonIcon = value;
                ButtonIconName = value.ToString();
                ButtonIconBundle = EditorIconsBundle.FontAwesome;
            }
        }

        public EnhancedInfoBoxAttribute(string message, InfoMessageType infoMessageType = InfoMessageType.Info, string details = null)
        {
            Message = message;
            InfoMessageType = infoMessageType;
            Details = details;
        }

        public EnhancedInfoBoxAttribute(string message, string details, InfoMessageType infoMessageType = InfoMessageType.Info) : this(message, infoMessageType, details)
        {
        }

        public EnhancedInfoBoxAttribute(string message, EditorIconsBundle bundle, string iconName, string details = null) : this(message, InfoMessageType.None, details)
        {
            Bundle = bundle;
            IconName = iconName;
        }

        public EnhancedInfoBoxAttribute(string message, FontAwesomeEditorIconType iconType, string details = null) : this(message, EditorIconsBundle.FontAwesome, iconType.ToString(), details)
        {
        }
    }
}
