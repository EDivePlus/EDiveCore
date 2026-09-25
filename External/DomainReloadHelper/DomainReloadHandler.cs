#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

namespace EDIVE.External.DomainReloadHelper
{
    // Resets marked statics when play mode skips the domain reload, and again on the way back to edit mode.
    public static class DomainReloadHandler
    {
        private const string LOG_PREFIX = "[" + nameof(DomainReloadHandler) + "] ";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void OnRuntimeLoad()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;

            if (EditorSettings.enterPlayModeOptionsEnabled && (EditorSettings.enterPlayModeOptions & EnterPlayModeOptions.DisableDomainReload) > 0)
                ReloadDomain();
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                ReloadDomain();
        }

        private static void ReloadDomain()
        {
            Profiler.BeginSample(nameof(DomainReloadHandler));
            var stopwatch = Stopwatch.StartNew();

            var executedMethods = 0;
            var clearedValues = 0;

            foreach (var member in DomainReloadMembers.Members)
            {
                if (member is MethodInfo method)
                {
                    if (TryExecute(method))
                        executedMethods++;
                    continue;
                }

                var attribute = member.GetCustomAttribute<ClearOnReloadAttribute>();
                if (Clear(member, attribute.Value, attribute.NewInstance))
                    clearedValues++;
            }

            stopwatch.Stop();
            Debug.Log($"{LOG_PREFIX}Executed {executedMethods} methods and cleared {clearedValues} values in {stopwatch.ElapsedMilliseconds} ms");

            Profiler.EndSample();
        }

        // Static field, property or event. Events always go back to null.
        public static bool Clear(MemberInfo member, object value = null, bool newInstance = false)
        {
            if (member == null)
                return false;

            if (!TryGetWriter(member, out var type, out var write))
            {
                Debug.LogWarning($"{LOG_PREFIX}{Describe(member)} has nothing to write to.");
                return false;
            }

            if (member is EventInfo)
            {
                value = null;
                newInstance = false;
            }

            try
            {
                write(newInstance ? Activator.CreateInstance(type)
                    : value == null || type.IsInstanceOfType(value) ? value
                    : Convert.ChangeType(value, type));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogWarning($"{LOG_PREFIX}Unable to clear {Describe(member)}.");
                return false;
            }
        }

        // The field itself, a property setter, or the compiler backing field of an event or get-only property.
        private static bool TryGetWriter(MemberInfo member, out Type type, out Action<object> write)
        {
            var field = member switch
            {
                FieldInfo memberField => memberField,
                EventInfo eventInfo => GetStaticField(eventInfo.DeclaringType, eventInfo.Name),
                PropertyInfo property when property.GetSetMethod(true) == null => GetStaticField(property.DeclaringType, $"<{property.Name}>k__BackingField"),
                _ => null
            };

            if (field != null)
            {
                type = field.FieldType;
                write = value => field.SetValue(null, value);
                return true;
            }

            var setter = (member as PropertyInfo)?.GetSetMethod(true);
            if (setter != null)
            {
                type = ((PropertyInfo) member).PropertyType;
                write = value => setter.Invoke(null, new[] { value });
                return true;
            }

            type = null;
            write = null;
            return false;
        }

        // One failing method must not stop the rest.
        private static bool TryExecute(MethodInfo method)
        {
            try
            {
                method.Invoke(null, null);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogWarning($"{LOG_PREFIX}Unable to execute {Describe(method)}.");
                return false;
            }
        }

        private static FieldInfo GetStaticField(Type type, string name)
        {
            return type?.GetField(name, BindingFlags.Static | BindingFlags.NonPublic);
        }

        internal static string Describe(MemberInfo member)
        {
            return $"{member.DeclaringType}.{member.Name}";
        }
    }

    // Everything marked for reload, in execution order. Built once per domain.
    internal static class DomainReloadMembers
    {
        private const BindingFlags DECLARED_MEMBERS = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        private static List<MemberInfo> _members;

        public static IReadOnlyList<MemberInfo> Members => _members ??= Collect();

        private static List<MemberInfo> Collect()
        {
            var marked = TypeCache.GetFieldsWithAttribute<ClearOnReloadAttribute>()
                .Cast<MemberInfo>()
                .Concat(FindPropertiesAndEvents())
                .Concat(TypeCache.GetMethodsWithAttribute<ExecuteOnReloadAttribute>())
                .Where(IsApplicable)
                .ToList();

            return CloseGenerics(marked)
                .OrderBy(member => member.GetCustomAttribute<DomainReloadHelperAttribute>().Order)
                .ToList();
        }

        // TypeCache has no lookup for these. Only assemblies referencing the attribute can use it.
        private static IEnumerable<MemberInfo> FindPropertiesAndEvents()
        {
            var helperAssembly = typeof(ClearOnReloadAttribute).Assembly;
            return GetAssembliesUsing(helperAssembly.GetName().Name)
                .SelectMany(GetLoadableTypes)
                .SelectMany(type => type.GetProperties(DECLARED_MEMBERS).Cast<MemberInfo>().Concat(type.GetEvents(DECLARED_MEMBERS)))
                .Where(member => member.IsDefined(typeof(ClearOnReloadAttribute), false));
        }

        // Instance members have no single value, generic methods no type arguments.
        private static bool IsApplicable(MemberInfo member)
        {
            var isStatic = member switch
            {
                FieldInfo field => field.IsStatic,
                PropertyInfo property => (property.GetMethod ?? property.SetMethod)?.IsStatic == true,
                EventInfo eventInfo => eventInfo.AddMethod?.IsStatic == true,
                MethodInfo method => method.IsStatic && !method.IsGenericMethod,
                _ => false
            };

            if (!isStatic)
                Debug.LogWarning($"[{nameof(DomainReloadMembers)}] Skipped {DomainReloadHandler.Describe(member)}, must be static and not a generic method.");
            return isStatic;
        }

        // A static on a generic type exists once per closed type. The ones in use show up as base types.
        private static IEnumerable<MemberInfo> CloseGenerics(List<MemberInfo> members)
        {
            var open = members.Where(member => member.DeclaringType?.ContainsGenericParameters == true).ToList();
            if (open.Count == 0)
                return members;

            var closedTypes = FindClosedTypes(open.Select(member => member.DeclaringType).ToHashSet());
            var closed = open.SelectMany(member => closedTypes[member.DeclaringType]
                .Select(closedType => FindOnClosedType(member, closedType))
                .Where(closedMember => closedMember != null));

            return members.Except(open).Concat(closed);
        }

        private static ILookup<Type, Type> FindClosedTypes(HashSet<Type> definitions)
        {
            return definitions
                .Select(definition => definition.Assembly.GetName().Name)
                .Distinct()
                .SelectMany(GetAssembliesUsing)
                .Distinct()
                .SelectMany(GetLoadableTypes)
                .SelectMany(GetBaseTypes)
                .Where(type => type.IsConstructedGenericType && !type.ContainsGenericParameters && definitions.Contains(type.GetGenericTypeDefinition()))
                .Distinct()
                .ToLookup(type => type.GetGenericTypeDefinition());
        }

        private static MemberInfo FindOnClosedType(MemberInfo member, Type closedType)
        {
            return closedType.GetMembers(DECLARED_MEMBERS)
                .FirstOrDefault(candidate => candidate.MetadataToken == member.MetadataToken && candidate.Module == member.Module);
        }

        private static IEnumerable<Type> GetBaseTypes(Type type)
        {
            for (var baseType = type.BaseType; baseType != null; baseType = baseType.BaseType)
                yield return baseType;
        }

        // The assembly itself and every assembly referencing it.
        private static IEnumerable<Assembly> GetAssembliesUsing(string assemblyName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => assembly.GetName().Name == assemblyName
                                   || assembly.GetReferencedAssemblies().Any(reference => reference.Name == assemblyName));
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(type => type != null);
            }
        }
    }
}
#endif
