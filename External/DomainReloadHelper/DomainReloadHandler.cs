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
            if (state == PlayModeStateChange.EnteredEditMode)
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
                    var reloadAttribute = field.GetCustomAttribute<ClearOnReloadAttribute>();
                    if (reloadAttribute == null)
                        continue;

                    if (reloadAttribute.AssignNewTypeInstance)
                    {
                        if (ClearFieldToNew(field)) 
                            clearedValues++;
                    }
                    else
                    {
                        if (ClearField(field, reloadAttribute.ValueToAssign)) 
                            clearedValues++;
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

        public static bool ClearField(FieldInfo field, object valueToAssign = null)
        {
            if (field == null)
                return false;
            try
            {
                var value = System.Convert.ChangeType(valueToAssign, field.FieldType);
                if (value == null)
                    Debug.LogWarning($"[{nameof(DomainReloadHandler)}] Unable to assign value of type {valueToAssign.GetType()} to field {field.Name} of type {field.FieldType}.");
                
                field.SetValue(null, value);
                return true;
            }
            catch
            {
                Debug.LogWarning($"[{nameof(DomainReloadHandler)}] Unable to clear field {field.Name} in class {field.DeclaringType?.Name}.");
                return false;
            }
        }
        
        public static bool ClearFieldToNew(FieldInfo field)
        {
            if (field == null)
                return false;
            try
            {
                var value = System.Activator.CreateInstance(field.FieldType);
                field.SetValue(null, value);
                return true;
            }
            catch
            {
                Debug.LogWarning($"[{nameof(DomainReloadHandler)}] Unable to clear field {field.Name}.");
                return false;
            }
        }
    }
}
#endif
