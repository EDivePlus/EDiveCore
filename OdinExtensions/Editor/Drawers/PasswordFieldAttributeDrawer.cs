using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    [DrawerPriority(DrawerPriorityLevel.WrapperPriority)]
    public class PasswordFieldAttributeDrawer : OdinAttributeDrawer<PasswordFieldAttribute, string>
    {
        private static readonly Color DelayedActiveColor = Color.yellow;
        private static int _localHotControl;
        private static string _delayedBuffer;

        protected override void DrawPropertyLayout(GUIContent label)
        {
            if (!Attribute.Delayed)
            {
                ValueEntry.SmartValue = EditorGUILayout.PasswordField(label, ValueEntry.SmartValue);
                return;
            }

            var value = ValueEntry.SmartValue ?? string.Empty;
            var rect = EditorGUILayout.GetControlRect();
            var controlId = GUIUtility.GetControlID(FocusType.Passive);

            if (OnLocalControlRelease(rect, controlId))
            {
                GUI.changed = true;
                value = _delayedBuffer;
            }

            var shown = value;
            if (_localHotControl == controlId)
            {
                GUIHelper.PushColor(DelayedActiveColor);
                shown = _delayedBuffer;
            }

            EditorGUI.BeginChangeCheck();
            var typed = label != null ? EditorGUI.PasswordField(rect, label, shown) : EditorGUI.PasswordField(rect, shown);
            if (_localHotControl == controlId)
                GUIHelper.PopColor();

            if (EditorGUI.EndChangeCheck())
            {
                GUI.changed = false;
                _localHotControl = controlId;
                _delayedBuffer = typed;
            }

            ValueEntry.SmartValue = value;
        }

        private static bool OnLocalControlRelease(Rect rect, int controlId)
        {
            if (_localHotControl == 0 || _localHotControl != controlId)
                return false;

            var e = Event.current;
            var released =
                e.rawType == EventType.MouseUp
                || (e.rawType == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
                || (e.rawType == EventType.MouseDown && e.button == 1)
                || (e.rawType == EventType.MouseDown && !rect.Contains(e.mousePosition));

            if (!released)
                return false;

            _localHotControl = 0;
            return true;
        }
    }
}
