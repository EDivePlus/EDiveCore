using System;
using System.Diagnostics;

namespace EDIVE.OdinExtensions.Attributes
{
    [Conditional("UNITY_EDITOR")]
    public class SceneReferenceAttribute : Attribute
    {
        public SceneReferenceType ReferenceType { get; set; }

        public bool OnlyBuildScenes { get; set; }

        public SceneReferenceAttribute(SceneReferenceType referenceType, bool onlyBuildScenes = false)
        {
            ReferenceType = referenceType;
            OnlyBuildScenes = onlyBuildScenes;
        }
    }
    
    public enum SceneReferenceType
    {
        Path,
        Name
    }
}
