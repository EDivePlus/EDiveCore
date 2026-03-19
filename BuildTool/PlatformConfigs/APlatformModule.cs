// Author: František Holubec
// Created: 18.03.2026

using System;
using UnityEditor;

namespace EDIVE.BuildTool.PlatformConfigs
{
    [Serializable]
    public abstract class APlatformModule : IPlatformModule, IComparable<APlatformModule>
    {
        public abstract string Label { get; }
        public virtual int ExecutionOrder => 0;
        
        public virtual bool SupportsTarget(BuildTarget target) => true;

        public virtual void SetupBeforeBuild(BuildContext context){}
        public virtual void RestoreAfterBuild(BuildContext context){}
        
        public int CompareTo(APlatformModule other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (other is null) return 1;
            var executionOrderComparison = ExecutionOrder.CompareTo(other.ExecutionOrder);
            if (executionOrderComparison != 0) return executionOrderComparison;
            return string.Compare(Label, other.Label, StringComparison.Ordinal);
        }
    }
}
