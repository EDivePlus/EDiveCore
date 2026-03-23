// Author: František Holubec
// Created: 18.03.2026

using System;
using EDIVE.OdinExtensions;
using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace EDIVE.BuildTool.PlatformConfigs
{
    [Serializable]
    public abstract class APlatformModule : IPlatformModule
    {
        public abstract string Label { get; }
        public virtual int Priority => 0;
        public string CallbackName => Label;

        public virtual bool SupportsTarget(BuildTarget target) => true;
        
        public int CompareTo(APlatformModule other)
        {
            return CompareTo((IBuildCallback) other);
        }

        public int CompareTo(IBuildCallback other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (other is null) return 1;
            return Priority.CompareTo(other.Priority);
        }

        public static bool TryGetModuleFromProperty<T>(InspectorProperty property, out T module) where T : IPlatformModule
        {
            module = default;
            return property != null && property.TryGetParentObject<BuildPlatformConfig>(out var config) && config.TryGetModule(out module);
        }
    }
}
