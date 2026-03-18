using System;
using System.Collections;
using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;
using UnityEditor;

namespace EDIVE.BuildTool.Actions
{
    [Serializable]
    public abstract class ABuildAction : IBuildAction
    {
        public virtual int Priority => 0;
        public virtual string Label => ObjectNames.NicifyVariableName(GetType().Name);
        public virtual string Tooltip => null;
        
        private const char TOOLTIP_ICON = '\u24d8';
        
        [OnInspectorGUI]
        [PropertyOrder(-100)]
        private void DrawLabel()
        {
            var tooltip = Tooltip;
            var content = string.IsNullOrEmpty(tooltip) ? GUIHelper.TempContent(Label) : GUIHelper.TempContent($"{Label} {TOOLTIP_ICON}", tooltip); 
            EditorGUILayout.LabelField(content, EditorStyles.boldLabel);
        }
        
        public int CompareTo(IBuildAction other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return Priority.CompareTo(other.Priority);
        }
    }
    
    public interface IBuildAction : IComparable<IBuildAction>
    {
        int Priority { get; }
        string Label { get; }
    }
    
    public interface IStateCaptureBuildAction : IBuildAction
    {
        IEnumerator OnStateCapture(BuildContext buildContext);
    }
    
    public interface IPreprocessBuildAction : IBuildAction
    {
        IEnumerator OnPreprocess(BuildContext buildContext);
    }
    
    public interface IPostprocessBuildAction : IBuildAction
    {
        IEnumerator OnPostprocess(BuildContext buildContext);
    }
    
    public interface IStateRestoreBuildAction : IBuildAction
    {
        IEnumerator OnStateRestore(BuildContext buildContext);
    }
}
