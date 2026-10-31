// Author: František Holubec
// Created: 30.10.2026

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.OdinExtensions.Editor
{
    // Draws T through the members of a facade type, so a type you don't own can be styled with attributes.
    // One facade per value, Target is never null. Members with [ShowInGroupHeader] go on the foldout row.
    // Covers subclasses of T. To share members across facades, make the base facade generic: BaseFacade<T> : OdinFacade<T> where T : Base.
    // Unity objects are only replaced as the inspected root, references to them stay object fields.
    [HideReferenceObjectPicker]
    public abstract class OdinFacade<T> : IOdinFacade where T : class
    {
        public T Target { get; internal set; }

        object IOdinFacade.Target => Target;
    }

    public interface IOdinFacade
    {
        object Target { get; }
    }

    // Binds a facade member to a Target member, by its own name or the given one. Only the facade member's attributes and order are used.
    // Works for private members and internal types the facade can't name.
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class FacadeMemberAttribute : ShowInInspectorAttribute
    {
        public readonly string Member;

        public FacadeMemberAttribute(string member = null) => Member = member;
    }

    public static class OdinFacadeUtility
    {
        public const string FACADE_PROPERTY = "Facade";

        private const BindingFlags MEMBER_FLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        internal static readonly MethodInfo CREATE_FACADE_INFO = typeof(OdinFacadeUtility).GetMethod(nameof(CreateFacadeInfo), BindingFlags.Static | BindingFlags.NonPublic);

        private static Dictionary<Type, Type> _facadeTypes;
        private static readonly Dictionary<Type, Type> RESOLVED = new();

        // Closest facade of the type or its base types
        public static Type GetFacadeType(Type targetType)
        {
            if (RESOLVED.TryGetValue(targetType, out var result))
                return result;

            _facadeTypes ??= FindFacadeTypes();
            for (var type = targetType; type != null && result == null; type = type.BaseType)
                _facadeTypes.TryGetValue(type, out result);

            RESOLVED[targetType] = result;
            return result;
        }

        public static Type GetTargetType(Type facadeType) => facadeType.GetArgumentsOfInheritedOpenGenericType(typeof(OdinFacade<>))[0];

        // A derived facade replaces its base for the same target
        private static Dictionary<Type, Type> FindFacadeTypes()
        {
            var result = new Dictionary<Type, Type>();
            foreach (var type in TypeCache.GetTypesDerivedFrom(typeof(OdinFacade<>)))
            {
                if (type.IsAbstract || type.ContainsGenericParameters)
                    continue;

                var target = GetTargetType(type);
                if (!result.TryGetValue(target, out var existing) || existing.IsAssignableFrom(type))
                    result[target] = type;
                else if (!type.IsAssignableFrom(existing))
                    Debug.LogWarning($"[OdinFacade] {target.Name} has more facades, using {existing.Name}, ignoring {type.Name}");
            }

            return result;
        }

        internal static InspectorPropertyInfo CreateFacadeInfo<TOwner, TTarget, TFacade>()
            where TOwner : class, TTarget where TTarget : class where TFacade : OdinFacade<TTarget>, new()
        {
            var getterSetter = new GetterSetter<TOwner, TFacade>(
                (ref TOwner owner) => owner == null ? null : FacadeCache<TTarget, TFacade>.Get(owner),
                (ref TOwner _, TFacade _) => { });
            return InspectorPropertyInfo.CreateValue(FACADE_PROPERTY, 0, SerializationBackend.None, getterSetter, new HideLabelAttribute());
        }

        internal static MemberInfo FindMember(Type type, string name)
        {
            for (; type != null; type = type.BaseType)
            {
                var member = (MemberInfo) type.GetField(name, MEMBER_FLAGS) ?? type.GetProperty(name, MEMBER_FLAGS);
                if (member != null)
                    return member;
            }

            return null;
        }

        private static class FacadeCache<TTarget, TFacade> where TTarget : class where TFacade : OdinFacade<TTarget>, new()
        {
            private static readonly ConditionalWeakTable<TTarget, TFacade> FACADES = new();

            public static TFacade Get(TTarget target) => FACADES.GetValue(target, t => new TFacade {Target = t});
        }
    }

    [ResolverPriority(100)]
    public class OdinFacadeResolver<T> : BaseMemberPropertyResolver<T> where T : class
    {
        public override bool CanResolveForPropertyFilter(InspectorProperty property)
        {
            if (OdinFacadeUtility.GetFacadeType(typeof(T)) == null)
                return false;
            return !typeof(Object).IsAssignableFrom(typeof(T)) || property == property.Tree.RootProperty;
        }

        protected override InspectorPropertyInfo[] GetPropertyInfos()
        {
            var facadeType = OdinFacadeUtility.GetFacadeType(typeof(T));
            var targetType = OdinFacadeUtility.GetTargetType(facadeType);
            return new[] {(InspectorPropertyInfo) OdinFacadeUtility.CREATE_FACADE_INFO.MakeGenericMethod(typeof(T), targetType, facadeType).Invoke(null, null)};
        }
    }

    // Swaps [FacadeMember] members for getters and setters on Target, before groups are built
    public class OdinFacadeMemberProcessor<TFacade> : OdinPropertyProcessor<TFacade> where TFacade : class, IOdinFacade
    {
        private static readonly MethodInfo CREATE_MEMBER_INFO = typeof(OdinFacadeMemberProcessor<TFacade>).GetMethod(nameof(CreateMemberInfo), BindingFlags.Static | BindingFlags.NonPublic);

        public override void ProcessMemberProperties(List<InspectorPropertyInfo> propertyInfos)
        {
            var targetType = OdinFacadeUtility.GetTargetType(typeof(TFacade));
            for (var i = 0; i < propertyInfos.Count; i++)
            {
                var info = propertyInfos[i];
                var attribute = info.Attributes.OfType<FacadeMemberAttribute>().FirstOrDefault();
                if (attribute == null)
                    continue;

                var name = attribute.Member ?? info.PropertyName;
                var member = OdinFacadeUtility.FindMember(targetType, name);
                if (member == null)
                {
                    Debug.LogError($"[OdinFacade] {typeof(TFacade).Name}: {targetType.Name} has no member '{name}'");
                    continue;
                }

                propertyInfos[i] = (InspectorPropertyInfo) CREATE_MEMBER_INFO.MakeGenericMethod(member.GetReturnType()).Invoke(null, new object[] {info, member});
            }
        }

        private static InspectorPropertyInfo CreateMemberInfo<TValue>(InspectorPropertyInfo info, MemberInfo member)
        {
            Func<object, object> get;
            Action<object, object> set = null;
            if (member is FieldInfo field)
            {
                get = field.GetValue;
                if (!field.IsInitOnly)
                    set = field.SetValue;
            }
            else
            {
                var property = (PropertyInfo) member;
                get = property.GetValue;
                if (property.GetSetMethod(true) != null)
                    set = property.SetValue;
            }

            var getterSetter = new GetterSetter<TFacade, TValue>(
                (ref TFacade facade) => (TValue) get(facade.Target),
                set == null ? null : (ref TFacade facade, TValue value) => set(facade.Target, value));
            return InspectorPropertyInfo.CreateValue(info.PropertyName, info.Order, SerializationBackend.None, getterSetter, info.Attributes);
        }
    }

    // Foldout row with the header members, the rest indented below
    public class OdinFacadeDrawer<T> : OdinValueDrawer<T> where T : class
    {
        private InspectorProperty _facade;
        private InspectorProperty[] _header;

        protected override bool CanDrawValueProperty(InspectorProperty property) =>
            property.ChildResolver is OdinFacadeResolver<T> && property != property.Tree.RootProperty;

        protected override void Initialize()
        {
            _facade = Property.Children[OdinFacadeUtility.FACADE_PROPERTY];
            _header = _facade?.Children.Where(c => c.GetAttribute<ShowInGroupHeaderAttribute>() != null).ToArray();
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            if (_facade == null || _header.Length == 0 || ValueEntry.SmartValue == null)
            {
                CallNextDrawer(label);
                return;
            }

            SirenixEditorGUI.BeginHorizontalPropertyLayout(label, out var labelRect);
            if (label != null)
                Property.State.Expanded = SirenixEditorGUI.Foldout(labelRect, Property.State.Expanded, GUIContent.none);
            foreach (var child in _header)
                child.Draw(null);
            SirenixEditorGUI.EndHorizontalPropertyLayout();

            if (SirenixEditorGUI.BeginFadeGroup(this, label == null || Property.State.Expanded))
            {
                GUIHelper.PushIndentLevel(EditorGUI.indentLevel + (label != null ? 1 : 0));
                foreach (var child in _facade.Children)
                {
                    if (!_header.Contains(child))
                        child.Draw();
                }

                GUIHelper.PopIndentLevel();
            }

            SirenixEditorGUI.EndFadeGroup();
        }
    }
}
#endif
