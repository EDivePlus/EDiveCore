using DG.Tweening;
using JetBrains.Annotations;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
#endif

namespace EDIVE.Tweening.Segments
{
    public interface ITweenSegment : ITweenTargetProvider
    {
        void AddToSequence(Sequence sequence);

#if UNITY_EDITOR
        string GetSummary();
        string LabelName { get; }
#endif
    }

#if UNITY_EDITOR
    [UsedImplicitly]
    public sealed class TweenSequenceSegmentDrawer<T> : OdinValueDrawer<T> where T : ITweenSegment
    {
        private GUIStyle _foldoutStyle;

        protected override void Initialize()
        {
            base.Initialize();
            _foldoutStyle = new GUIStyle(SirenixGUIStyles.Foldout)
            {
               // TODO fix clipping
            };
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            SirenixEditorGUI.BeginBox();
            Property.State.Expanded = SirenixEditorGUI.Foldout(Property.State.Expanded, GUIHelper.TempContent(ValueEntry.SmartValue.GetSummary()), _foldoutStyle);
            if (SirenixEditorGUI.BeginFadeGroup(this, Property.State.Expanded))
            {
                CallNextDrawer(label);
            }
            SirenixEditorGUI.EndFadeGroup();
            SirenixEditorGUI.EndBox();
        }
    }
#endif
}
