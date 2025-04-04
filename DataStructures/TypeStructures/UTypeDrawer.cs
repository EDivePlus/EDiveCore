#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace EDIVE.DataStructures.TypeStructures
{
    public sealed class UTypeDrawer : OdinValueDrawer<UType>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            Property.Children["Type"].Draw(label);
        }
    }
    
    [ResolverPriority(-100000)]
    public class UTypeDrawerAttributeProcessor : OdinAttributeProcessor<UType>
    {
        public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
        {
            if (member.Name != "Type")
                return;

            foreach (var parentAttribute in parentProperty.Attributes)
            {
                if (parentAttribute is TypeSelectorSettingsAttribute)
                    attributes.Add(parentAttribute);
            }
        }
    }
}
#endif