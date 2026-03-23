using System;
using System.Collections;
using UnityEditor;

namespace EDIVE.BuildTool.Actions
{
    [Serializable]
    public abstract class ABuildAction : IBuildAction
    {
        public virtual int Priority => 0;
        public virtual string Label => ObjectNames.NicifyVariableName(GetType().Name);
        public virtual string Tooltip => null;

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
        string Tooltip { get; }
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
