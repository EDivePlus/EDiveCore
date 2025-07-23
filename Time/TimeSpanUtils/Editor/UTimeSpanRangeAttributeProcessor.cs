#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;

namespace EDIVE.Time.TimeSpanUtils.Editor
{
    [ResolverPriority(-100000)]
    public class UTimeSpanRangeAttributeProcessor : OdinAttributeProcessor<UTimeSpanRange>
    {
        public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
        {
            var setting = parentProperty.GetAttribute<TimeSpanDrawerSettingsAttribute>();
            if (setting == null)
                return;

            if (member.GetReturnType() == typeof(UTimeSpan))
            {
                attributes.Add(setting);
            }
        }
    }
}
#endif
