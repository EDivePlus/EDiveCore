// Source: https://forum.unity.com/threads/attribute-to-clear-static-fields-on-play-start.790226/
// https://github.com/joshcamas/unity-domain-reload-helper/tree/master

using JetBrains.Annotations;

namespace EDIVE.External.DomainReloadHelper
{
    [System.AttributeUsage(System.AttributeTargets.Method)]
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [MeansImplicitUse]
    public class ExecuteOnReloadAttribute : DomainReloadHelperAttribute
    {
        public ExecuteOnReloadAttribute(int order) : base(order) { }

        public ExecuteOnReloadAttribute() { }
    }
}
