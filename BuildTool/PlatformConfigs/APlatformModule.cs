// Author: František Holubec
// Created: 18.03.2026

using System;
using System.Collections.Generic;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions;
using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace EDIVE.BuildTool.PlatformConfigs
{
    [Serializable]
    public abstract class APlatformModule : IComparable<APlatformModule>
    {
        public abstract string Label { get; }
        public virtual int ExecutionOrder => 0;
        
        public virtual bool SupportsTarget(BuildTarget target) => true;

        public virtual void SetupBeforeBuild(BuildContext context){}
        public virtual void RestoreAfterBuild(BuildContext context){}

        protected bool Equals(APlatformModule other)
        {
            return GetType() == other?.GetType();
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((APlatformModule) obj);
        }

        public override int GetHashCode()
        {
            return GetType().GetHashCode();
        }

        public int CompareTo(APlatformModule other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (other is null) return 1;
            var executionOrderComparison = ExecutionOrder.CompareTo(other.ExecutionOrder);
            if (executionOrderComparison != 0) return executionOrderComparison;
            return string.Compare(Label, other.Label, StringComparison.Ordinal);
        }

        protected bool TryGetSiblingModule<T>(InspectorProperty property, out T module) where T : APlatformModule
        {
            module = null;
            return property.TryGetParentObject<IEnumerable<APlatformModule>>(out var collection) && collection.TryGetFirstT(out module);
        }
    }
    
    
}
