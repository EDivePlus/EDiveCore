#if UNITY_EDITOR
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace EDIVE.External.DomainReloadHelper
{
    public static class DomainReloadHandler
    {
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
            if (state == PlayModeStateChange.ExitingPlayMode) 
                ReloadDomain();
        }
        
        private static void ReloadDomain()
        {
            Profiler.BeginSample("DomainReloadHandler");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var executedMethods = 0;
            var clearedValues = 0;

            var orderedMembers = TypeCache.GetFieldsWithAttribute<ClearOnReloadAttribute>()
                .Where(FilterFields)
                .Cast<MemberInfo>()
                .Concat(TypeCache.GetMethodsWithAttribute<ExecuteOnReloadAttribute>())
                .OrderBy(info => info.GetCustomAttribute<DomainReloadHelperAttribute>().Order);

            foreach (var member in orderedMembers)
            {
                if (member is MethodInfo method)
                {
                    if (method.IsGenericMethod || !method.IsStatic) continue;

                    method.Invoke(null, new object[] { });
                    executedMethods++;
                }
                else if (member is FieldInfo field)
                {
                    var fieldType = field.FieldType;

                    // Extract attribute and access its parameters
                    var reloadAttribute = field.GetCustomAttribute<ClearOnReloadAttribute>();
                    if (reloadAttribute == null)
                        continue;
                    var valueToAssign = reloadAttribute.ValueToAssign;
                    var assignNewTypeInstance = reloadAttribute.AssignNewTypeInstance;

                    // Use valueToAssign only if it's convertible to the field value type
                    object value = null;
                    if (valueToAssign != null)
                    {
                        value = System.Convert.ChangeType(valueToAssign, fieldType);
                        if (value == null)
                            Debug.LogWarning($"[{nameof(DomainReloadHandler)}] Unable to assign value of type {valueToAssign.GetType()} to field {field.Name} of type {fieldType}.");
                    }

                    // If assignNewTypeInstance is set, create a new instance of this type and assign it to the field
                    if (assignNewTypeInstance)
                        value = System.Activator.CreateInstance(fieldType);

                    try
                    {
                        field.SetValue(null, value);
                        clearedValues++;
                    }
                    catch
                    {
                        if (valueToAssign == null)
                            Debug.LogWarning($"[{nameof(DomainReloadHandler)}] Unable to clear field {field.Name}.");
                        else
                            Debug.LogWarning($"[{nameof(DomainReloadHandler)}] Unable to assign field {field.Name}.");
                    }
                }
            }

            stopwatch.Stop();

            Debug.Log($"[{nameof(DomainReloadHandler)}] Executed {executedMethods} methods and cleared {clearedValues} values in {stopwatch.ElapsedMilliseconds} ms");

            Profiler.EndSample();
        }

        private static bool FilterFields(FieldInfo field)
        {
            var filterValue = field != null && !field.FieldType.IsGenericParameter && field.IsStatic;
            if (field != null && !filterValue)
                Debug.LogWarning($"[{nameof(DomainReloadHandler)}] Inapplicable field {field.Name} to clear; must be static and non-generic.");
            return filterValue;
        }
    }
}
#endif
