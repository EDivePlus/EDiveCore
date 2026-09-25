// Source: https://forum.unity.com/threads/attribute-to-clear-static-fields-on-play-start.790226/
// https://github.com/joshcamas/unity-domain-reload-helper/tree/master

using System;

namespace EDIVE.External.DomainReloadHelper
{
    // Resets a static field, property or event when play mode skips the domain reload.
    // Default value unless Value or NewInstance says otherwise. On a generic type, every closed type in use is reset.
    //
    //   [ClearOnReload] static Foo _instance;
    //   [ClearOnReload(Value = 1)] static int _count;
    //   [ClearOnReload(NewInstance = true)] static readonly List<Foo> CACHE = new();
    //   [ClearOnReload] static event Action Changed;
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event)]
    public class ClearOnReloadAttribute : DomainReloadHelperAttribute
    {
        // Converted to the member type. Ignored on events.
        public object Value { get; set; }

        // New object of the member type. Ignored on events.
        public bool NewInstance { get; set; }

        public ClearOnReloadAttribute() { }

        public ClearOnReloadAttribute(int order) : base(order) { }
    }
}
