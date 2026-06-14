using System;
using System.Diagnostics;
using UnityEngine;

namespace EDIVE.OdinExtensions.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    [Conditional("UNITY_EDITOR")]
    public class AnimatorParameterAttribute : PropertyAttribute
    {
        public string AnimatorGetter { get; }
        public AnimatorControllerParameterType[] FilterTypes { get; }

        public AnimatorParameterAttribute() { }
        
        public AnimatorParameterAttribute(params AnimatorControllerParameterType[] filterTypes)
        {
            FilterTypes = filterTypes ?? Array.Empty<AnimatorControllerParameterType>();
        }
        
        public AnimatorParameterAttribute(string animatorGetter = null, params AnimatorControllerParameterType[] filterTypes)
        {
            AnimatorGetter = animatorGetter;
            FilterTypes = filterTypes ?? Array.Empty<AnimatorControllerParameterType>();
        }
    }
}
