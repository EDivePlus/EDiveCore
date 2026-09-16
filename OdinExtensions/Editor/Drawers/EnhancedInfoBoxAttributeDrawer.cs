// Author: František Holubec
// Created: 16.09.2026

using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ActionResolvers;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    [DrawerPriority(0, 10001, 0)]
    public sealed class EnhancedInfoBoxAttributeDrawer : OdinAttributeDrawer<EnhancedInfoBoxAttribute>
    {
        private const float ICON_SIZE = 20;
        private const float SDF_ICON_PADDING = 2;
        private const float ARROW_SIZE = 18;
        private const float SPACING = 4;
        private const float BUTTON_HEIGHT = 18;
        private const float BUTTON_ICON_SIZE = 14;

        private static GUIStyle _boxStyle;
        private static GUIStyle _boxWithArrowStyle;
        private static GUIStyle _messageStyle;
        private static GUIStyle _detailsStyle;

        private static GUIStyle BoxStyle => _boxStyle ??= new GUIStyle(SirenixGUIStyles.MessageBox);

        private static GUIStyle BoxWithArrowStyle => _boxWithArrowStyle ??= new GUIStyle(BoxStyle)
        {
            padding = new RectOffset(BoxStyle.padding.left, BoxStyle.padding.right + (int) ARROW_SIZE, BoxStyle.padding.top, BoxStyle.padding.bottom)
        };

        private static GUIStyle MessageStyle => _messageStyle ??= CreateTextStyle(TextAnchor.MiddleLeft);
        private static GUIStyle DetailsStyle => _detailsStyle ??= CreateTextStyle(TextAnchor.UpperLeft);

        private ValueResolver<bool> _showIfResolver;
        private ValueResolver<string> _messageResolver;
        private ValueResolver<string> _detailsResolver;
        private ValueResolver<string> _iconNameResolver;
        private ValueResolver<Color> _iconColorResolver;
        private ValueResolver<string> _buttonLabelResolver;
        private ValueResolver<string> _buttonTooltipResolver;
        private ValueResolver<string> _buttonIconNameResolver;
        private ActionResolver _buttonAction;
        private string _actionName;

        private bool _show;
        private bool _expanded;
        private string _message;
        private string _details;
        private BoxIcon _icon;
        private string _buttonLabel;
        private string _buttonTooltip;
        private EditorIcon _buttonIcon;

        protected override void Initialize()
        {
            _showIfResolver = ValueResolver.Get(Property, Attribute.ShowIf, true);
            _messageResolver = ValueResolver.GetForString(Property, Attribute.Message);
            _detailsResolver = ValueResolver.GetForString(Property, Attribute.Details);
            _iconNameResolver = ValueResolver.GetForString(Property, Attribute.IconName);
            _iconColorResolver = ValueResolver.Get<Color>(Property, Attribute.IconColor);
            _buttonLabelResolver = ValueResolver.GetForString(Property, Attribute.ButtonLabel);
            _buttonTooltipResolver = ValueResolver.GetForString(Property, Attribute.ButtonTooltip);
            _buttonIconNameResolver = ValueResolver.GetForString(Property, Attribute.ButtonIconName);

            if (Attribute.ButtonAction != null)
            {
                _buttonAction = ActionResolver.Get(Property, Attribute.ButtonAction);
                _actionName = Attribute.ButtonAction.SplitPascalCase();
            }

            _expanded = Attribute.Expanded;
            Resolve();
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            ValueResolver.DrawErrors(_showIfResolver, _messageResolver, _detailsResolver, _iconNameResolver, _iconColorResolver, _buttonLabelResolver, _buttonTooltipResolver, _buttonIconNameResolver);
            if (_buttonAction != null)
                ActionResolver.DrawErrors(_buttonAction);

            if (_showIfResolver.HasError || _messageResolver.HasError)
            {
                CallNextDrawer(label);
                return;
            }

            if (Event.current.type == EventType.Layout)
                Resolve();

            if (_show)
            {
                if (Attribute.GUIAlwaysEnabled) GUIHelper.PushGUIEnabled(true);
                GUIHelper.PushIsBoldLabel(false);

                _expanded = MessageBox(_message, _details, _icon, _expanded, _buttonLabel, _buttonIcon, _buttonTooltip, out var clicked);

                GUIHelper.PopIsBoldLabel();
                if (Attribute.GUIAlwaysEnabled) GUIHelper.PopGUIEnabled();

                if (clicked && _buttonAction != null)
                {
                    Property.RecordForUndo($"Click {_actionName}");
                    _buttonAction.DoActionForAllSelectionIndices();
                    Property.MarkSerializationRootDirty();
                }
            }

            CallNextDrawer(label);
        }

        private void Resolve()
        {
            _show = !_showIfResolver.HasError && _showIfResolver.GetValue();
            if (!_show)
                return;

            _message = _messageResolver.HasError ? null : _messageResolver.GetValue();
            _details = _detailsResolver.HasError ? null : _detailsResolver.GetValue();
            _icon = ResolveIcon();
            ResolveButton();
        }

        private BoxIcon ResolveIcon()
        {
            Color? color = Attribute.IconColor != null && !_iconColorResolver.HasError ? _iconColorResolver.GetValue() : null;
            var iconName = _iconNameResolver.HasError ? null : _iconNameResolver.GetValue();
            var icon = EditorIconsUtility.GetIcon(iconName, Attribute.Bundle);
            if (icon != null)
                return new BoxIcon(icon, Attribute.IconType, color);

            var type = Attribute.InfoMessageType;
            return new BoxIcon(GetDefaultIcon(type), color ?? GetDefaultColor(type));
        }

        private void ResolveButton()
        {
            _buttonLabel = null;
            _buttonTooltip = null;
            _buttonIcon = null;
            if (_buttonAction == null || _buttonAction.HasError)
                return;

            var iconName = _buttonIconNameResolver.HasError ? null : _buttonIconNameResolver.GetValue();
            _buttonIcon = EditorIconsUtility.GetIcon(iconName, Attribute.HasButtonIconBundle ? Attribute.ButtonIconBundle : null);
            _buttonLabel = _buttonLabelResolver.HasError ? null : _buttonLabelResolver.GetValue();
            if (string.IsNullOrEmpty(_buttonLabel) && _buttonIcon == null)
                _buttonLabel = _actionName;

            _buttonTooltip = _buttonTooltipResolver.HasError ? null : _buttonTooltipResolver.GetValue();
            if (string.IsNullOrEmpty(_buttonTooltip) && string.IsNullOrEmpty(_buttonLabel))
                _buttonTooltip = _actionName;
        }

        public static SdfIconType GetDefaultIcon(InfoMessageType type) => type switch
        {
            InfoMessageType.Info => SdfIconType.InfoCircleFill,
            InfoMessageType.Warning => SdfIconType.ExclamationTriangleFill,
            InfoMessageType.Error => SdfIconType.ExclamationOctagonFill,
            _ => SdfIconType.None
        };

        public static Color GetDefaultColor(InfoMessageType type) => type switch
        {
            InfoMessageType.Warning => SirenixGUIStyles.YellowWarningColor,
            InfoMessageType.Error => SirenixGUIStyles.RedErrorColor,
            _ => SirenixGUIStyles.HighlightedTextColor
        };

        private static GUIStyle CreateTextStyle(TextAnchor alignment) => new(EditorStyles.label)
        {
            fontSize = BoxStyle.fontSize,
            richText = true,
            wordWrap = true,
            alignment = alignment,
            margin = new RectOffset(0, 0, 0, 0),
            padding = new RectOffset(0, 0, 0, 0),
            normal = {textColor = BoxStyle.normal.textColor}
        };

        public static bool MessageBox(string message, string details, InfoMessageType type, bool expanded)
        {
            return MessageBox(message, details, new BoxIcon(GetDefaultIcon(type), GetDefaultColor(type)), expanded, null, null, null, out _);
        }

        public static bool MessageBox(string message, string details, BoxIcon icon, bool expanded,
            string buttonText, EditorIcon buttonIcon, string buttonTooltip, out bool buttonClicked)
        {
            buttonClicked = false;
            var hasDetails = !string.IsNullOrEmpty(details);
            var hasButtonText = !string.IsNullOrEmpty(buttonText);
            var hasButton = hasButtonText || buttonIcon != null;
            if (hasButtonText && buttonIcon != null)
                buttonText = " " + buttonText;

            var outRect = EditorGUILayout.BeginHorizontal(hasDetails ? BoxWithArrowStyle : BoxStyle);

            var iconRect = Rect.zero;
            if (!icon.IsEmpty)
            {
                iconRect = GUILayoutUtility.GetRect(ICON_SIZE, ICON_SIZE, GUILayoutOptions.ExpandWidth(false).ExpandHeight(false));
                GUILayout.Space(SPACING);
            }

            EditorGUILayout.BeginVertical();
            var messageContent = GUIHelper.TempContent(message);
            var messageRect = GUILayoutUtility.GetRect(messageContent, MessageStyle, GUILayoutOptions.MinHeight(ICON_SIZE));
            GUI.Label(messageRect, messageContent, MessageStyle);
            if (hasDetails && expanded)
            {
                var detailsContent = GUIHelper.TempContent(details);
                var detailsRect = GUILayoutUtility.GetRect(detailsContent, DetailsStyle);
                GUI.Label(detailsRect, detailsContent, DetailsStyle);
            }
            EditorGUILayout.EndVertical();

            var buttonRect = Rect.zero;
            if (hasButton)
            {
                var buttonWidth = hasButtonText
                    ? GUI.skin.button.CalcSize(GUIHelper.TempContent(buttonText)).x + (buttonIcon != null ? BUTTON_ICON_SIZE : 0) + 10
                    : BUTTON_HEIGHT;

                GUILayout.Space(SPACING);
                buttonRect = GUILayoutUtility.GetRect(buttonWidth, BUTTON_HEIGHT, GUILayoutOptions.ExpandWidth(false).ExpandHeight(false));
                buttonRect = buttonRect.SetCenterY(messageRect.center.y);
            }
            EditorGUILayout.EndHorizontal();

            if (hasButton)
                buttonClicked = DrawButton(buttonRect, buttonText, buttonIcon, buttonTooltip);

            var e = Event.current;
            if (e.type == EventType.Repaint)
            {
                if (!icon.IsEmpty)
                    icon.Draw(iconRect.SetCenterY(messageRect.center.y));

                if (hasDetails)
                {
                    var arrowRect = new Rect(outRect.xMax - BoxStyle.padding.right - ARROW_SIZE, messageRect.center.y - ARROW_SIZE * 0.5f, ARROW_SIZE, ARROW_SIZE);
                    var arrow = expanded ? EditorIcons.TriangleUp : EditorIcons.TriangleDown;
                    GUI.DrawTexture(arrowRect, arrow.Active, ScaleMode.ScaleToFit);
                }
            }

            if (hasDetails)
                EditorGUIUtility.AddCursorRect(outRect, MouseCursor.Link);

            if (e.rawType != EventType.MouseDown || !outRect.Contains(e.mousePosition) || buttonRect.Contains(e.mousePosition))
                return expanded;

            if (e.button == 0 && hasDetails)
            {
                expanded = !expanded;
                UseEvent(e);
            }
            else if (e.button == 1)
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Copy message"), false, () => Clipboard.Copy(message));
                if (hasDetails)
                    menu.AddItem(new GUIContent("Copy details"), false, () => Clipboard.Copy(details));
                menu.ShowAsContext();
                UseEvent(e);
            }

            return expanded;
        }

        private static void UseEvent(Event e)
        {
            GUIHelper.PushGUIEnabled(true);
            e.Use();
            GUIHelper.PopGUIEnabled();
        }

        private static bool DrawButton(Rect rect, string text, EditorIcon icon, string tooltip)
        {
            if (string.IsNullOrEmpty(text))
                return SirenixEditorGUI.IconButton(rect, icon, tooltip);

            var image = icon != null && Event.current.type != EventType.Layout ? icon.Highlighted : null;
            var prevIconSize = EditorGUIUtility.GetIconSize();
            EditorGUIUtility.SetIconSize(new Vector2(BUTTON_ICON_SIZE, BUTTON_ICON_SIZE));
            var clicked = GUI.Button(rect, GUIHelper.TempContent(text, image, tooltip));
            EditorGUIUtility.SetIconSize(prevIconSize);
            return clicked;
        }

        public readonly struct BoxIcon
        {
            private readonly Texture _texture;
            private readonly EditorIcon _editorIcon;
            private readonly EditorIconTextureType _textureType;
            private readonly SdfIconType _sdfIcon;
            private readonly Color? _color;

            public bool IsEmpty => _sdfIcon == SdfIconType.None && _editorIcon == null && _texture == null;

            public BoxIcon(SdfIconType icon, Color? color = null) : this()
            {
                _sdfIcon = icon;
                _color = color;
            }

            public BoxIcon(EditorIcon icon, EditorIconTextureType textureType = EditorIconTextureType.Highlighted, Color? color = null) : this()
            {
                _editorIcon = icon;
                _textureType = textureType;
                _color = color;
            }

            public BoxIcon(Texture texture, Color? color = null) : this()
            {
                _texture = texture;
                _color = color;
            }

            public void Draw(Rect rect)
            {
                if (_sdfIcon != SdfIconType.None)
                {
                    SdfIcons.DrawIcon(rect.Padding(SDF_ICON_PADDING), _sdfIcon, _color ?? SirenixGUIStyles.HighlightedTextColor);
                    return;
                }

                var texture = _editorIcon == null ? _texture : _color.HasValue ? _editorIcon.Raw : _editorIcon.GetTexture(_textureType);
                if (texture != null)
                    GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true, 0, _color ?? Color.white, 0, 0);
            }
        }
    }
}
