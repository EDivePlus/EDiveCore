using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
#endif

namespace EDIVE.DataStructures
{
    [Serializable]
    public sealed class UType : ISerializationCallbackReceiver
    {
        [SerializeField]
        private string _ClassRef;
        public string ClassRef => _ClassRef;

        [ShowInInspector]
        public Type Type
        {
            get => _type;
            set
            {
                _type = value;
                _ClassRef = GetClassRef(value);
            }
        }
        private Type _type;

        public UType(Type type)
        {
            Type = type;
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (!string.IsNullOrEmpty(ClassRef))
            {
                _type = Type.GetType(ClassRef);

                if (_type == null)
                    Debug.LogWarning($"'{ClassRef}' was referenced but class type was not found.");
            }
            else
            {
                _type = null;
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        public static string GetClassRef(Type type)
        {
            return type != null
                ? type.FullName + ", " + type.Assembly.GetName().Name
                : "";
        }
        
        public static implicit operator Type(UType typeReference)
        {
            return typeReference?.Type;
        }

        public static implicit operator UType(Type type)
        {
            return new UType(type);
        }

        public override string ToString()
        {
            return Type != null && Type.FullName != null ? Type.FullName : "(None)";
        }

        private bool Equals(UType other)
        {
            return _type == other._type;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is UType other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (_type != null ? _type.GetHashCode() : 0);
        }
    }

#if UNITY_EDITOR
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
#endif
}
