using System;
using UnityEditor;

namespace EDIVE.BuildTool.Actions
{
    [Serializable]
    public abstract class ABuildAction : IBuildCallback
    {
        public virtual int Priority => 0;
        public virtual string CallbackName => ObjectNames.NicifyVariableName(GetType().Name);
        public virtual string Tooltip => null;
        
        public int CompareTo(IBuildCallback other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return Priority.CompareTo(other.Priority);
        }
    }
}
