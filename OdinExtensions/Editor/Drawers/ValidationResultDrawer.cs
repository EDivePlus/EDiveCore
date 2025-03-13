#if UNITY_EDITOR
using System;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.Validation;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    public class ValidationResultDrawer : OdinValueDrawer<ValidationResult>, IDisposable
    {
        private static Color HighlightedBgColor => EditorGUIUtility.isProSkin ? Color.Lerp(EditorWindowBgColor, Color.white, 0.15f) : new Color(239 / 255f, 239 / 255f, 239 / 255f, 1);
        private static Color EditorWindowBgColor => EditorGUIUtility.isProSkin ? DarkSkinEditorWindowBgColor : new Color(0.76f, 0.76f, 0.76f, 1f);
        private static Color DarkSkinEditorWindowBgColor => new(0.22f, 0.22f, 0.22f, 1f);

        private PropertyTree _issueFixerTree;
        private PropertyTree _metaDataTree;

        private GUIStyle _messageBoxText;
        private GUIStyle MessageBoxText => _messageBoxText ??= new GUIStyle("label")
        {
            margin = new RectOffset(4, 4, 2, 2),
            fontSize = 10,
            richText = true,
            wordWrap = true,
        };

        private static readonly Type METADATA_TYPE = typeof(InspectorProperty).Assembly.GetType("Sirenix.OdinInspector.Editor.Validation.ResultItemMetaDataDrawer");

        protected override void Initialize()
        {
            _issueFixerTree?.Dispose();
            var fix = ValueEntry.SmartValue[0].Fix;
            var fixHasArguments = fix?.ArgType != null;

            if (!fixHasArguments) return;

            var editorObject = fix.CreateEditorObject();
            _issueFixerTree = PropertyTree.Create(editorObject);
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            GUILayout.BeginVertical();

            var result = ValueEntry.SmartValue;
            var messageType = result.ResultType switch
            {
                ValidationResultType.Error => MessageType.Error,
                ValidationResultType.Warning => MessageType.Warning,
                ValidationResultType.Valid when !string.IsNullOrEmpty(result.Message) => MessageType.Info,
                _ => MessageType.None
            };

            if (messageType != MessageType.None)
            {
                DrawMessageBoxWithButton(ref result[0], messageType, result[0].OnContextClick);
            }

            GUILayout.EndVertical();
        }

        public void Dispose()
        {
            _issueFixerTree?.Dispose();
            _metaDataTree?.Dispose();
            _issueFixerTree = null;
            _metaDataTree = null;
        }

        private void DrawMessageBoxWithButton(ref ResultItem entry, MessageType messageType, Action<GenericMenu> onContextClick)
        {
            var icon = messageType switch
            {
                MessageType.Info => EditorIcons.UnityInfoIcon,
                MessageType.Warning => EditorIcons.UnityWarningIcon,
                MessageType.Error => EditorIcons.UnityErrorIcon,
                _ => (Texture) null
            };

            var btnCount = 0;
            var firstButtonIndex = -1;

            if (entry.MetaData != null && entry.MetaData.Length > 0)
            {
                for (var i = 0; i < entry.MetaData.Length; i++)
                {
                    if (entry.MetaData[i].Value is not Action)
                        continue;

                    if (btnCount == 0)
                        firstButtonIndex = i;

                    btnCount++;
                }

                if (_metaDataTree == null && btnCount < entry.MetaData.Length)
                {
                    var metadata = Activator.CreateInstance(METADATA_TYPE, entry.MetaData, btnCount == 1);
                    _metaDataTree = PropertyTree.Create(metadata);
                }
            }

            var entireBox = SirenixEditorGUI.BeginVerticalWithoutUsingControlID(SirenixGUIStyles.MessageBox);
            if (btnCount == 1)
            {
                SirenixEditorGUI.BeginHorizontalWithoutUsingControlID(SirenixGUIStyles.None);
            }
            var messageRect = EditorGUILayout.BeginVertical(SirenixGUIStyles.None);
            GUILayout.Label(GUIHelper.TempContent(entry.Message, icon), MessageBoxText);
            EditorGUILayout.EndVertical();
            if (btnCount == 1)
            {
                ref var firstButton = ref entry.MetaData[firstButtonIndex];
                var btnWidth = GUI.skin.button.CalcSize(GUIHelper.TempContent(firstButton.Name)).x + 10;

                GUILayout.BeginVertical(GUILayoutOptions.Width(btnWidth));
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(firstButton.Name, GUILayoutOptions.Width(btnWidth)))
                {
                    (firstButton.Value as Action)?.Invoke();
                    Property.ForceMarkDirty();
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }

            var showFix = entry.Fix != null && entry.Fix.OfferInInspector;
            if (showFix)
            {
                var fixTitle = new GUIContent(entry.Fix.Title ?? "Fix");
                var fixRect = EditorGUILayout.GetControlRect(false, 20f).Expand(4f, 4f, 0f, 3f);

                SirenixEditorGUI.DrawBorders(fixRect, 0, 0, 1, 1);

                const float buttonSize = 85f;
                var fixTitleRect = fixRect.AlignLeft(fixRect.width - buttonSize).HorizontalPadding(6f);
                GUI.Label(fixTitleRect, fixTitle, SirenixGUIStyles.Label);

                var buttonRect = fixRect.AlignRight(buttonSize);
                var paddedButtonRect = buttonRect.HorizontalPadding(6f);
                var evt = Event.current;

                EditorGUI.DrawRect(buttonRect.Padding(1f), buttonRect.Contains(evt.mousePosition)
                    ? HighlightedBgColor
                    : Color.clear);

                GUI.Label(paddedButtonRect.AlignRight(50f), "Fix now", SirenixGUIStyles.LabelCentered);
                SdfIcons.DrawIcon(paddedButtonRect.AlignLeft(20f).Padding(3f), SdfIconType.Tools);
                EditorGUI.DrawRect(buttonRect.AlignLeft(1f), SirenixGUIStyles.BorderColor);

                var fixHasArguments = entry.Fix.ArgType != null;
                if (fixHasArguments && _issueFixerTree == null)
                {
                    var editorObject = entry.Fix.CreateEditorObject();
                    _issueFixerTree = PropertyTree.Create(editorObject);
                }

                if (evt.OnMouseUp(buttonRect, 0))
                {
                    if (fixHasArguments)
                    {
                        var args = _issueFixerTree.WeakTargets[0];
                        entry.Fix.Action.DynamicInvoke(args);
                    }
                    else
                    {
                        entry.Fix.Action.DynamicInvoke();
                    }
                    Property.ForceMarkDirty();
                    GUIHelper.ExitGUI(true);
                }
            }

            if (Event.current.OnMouseUp(messageRect, 1))
            {
                var menu = new GenericMenu();
                var message = entry.Message;
                menu.AddItem(new GUIContent("Copy message"), false, () => { Clipboard.Copy(message); });
                onContextClick?.Invoke(menu);
                menu.ShowAsContext();
                Property.ForceMarkDirty();
                Event.current.Use();
            }

            if (_issueFixerTree != null)
            {
                EditorGUILayout.BeginVertical(SirenixGUIStyles.ContentPadding);
                _issueFixerTree.Draw(false);
                EditorGUILayout.EndVertical();
            }

            if (_metaDataTree != null)
            {
                if (_issueFixerTree != null)
                {
                    GUILayout.Space(2);
                    var header = SirenixEditorGUI.BeginToolbarBoxHeader();
                    EditorGUI.DrawRect(header.AlignTop(1f).SetWidth(entireBox.width).SetX(entireBox.x), SirenixGUIStyles.BorderColor);
                    GUILayout.Label("Metadata");
                    SirenixEditorGUI.EndToolbarBoxHeader();
                }

                EditorGUILayout.BeginVertical(SirenixGUIStyles.ContentPadding);
                _metaDataTree.Draw(false);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }
    }
}
#endif
