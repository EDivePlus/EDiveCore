#if UNITY_EDITOR
using System;
using System.Reflection;
using EDIVE.NativeUtils;

namespace EDIVE.EditorUtils.AnnotationUtility
{
    public class AnnotationWrapper
    {
        private static readonly FieldInfo ICON_ENABLED_FIELD;
        private static readonly FieldInfo GIZMO_ENABLED_FIELD;
        private static readonly FieldInfo FLAGS_FIELD;
        private static readonly FieldInfo CLASS_ID_FIELD;
        private static readonly FieldInfo SCRIPT_CLASS_FIELD;

        private static readonly MethodInfo SET_ICON_ENABLED_METHOD;
        private static readonly MethodInfo SET_GIZMO_ENABLED_METHOD;

        static AnnotationWrapper()
        {
            var annotationType = Type.GetType("UnityEditor.Annotation, UnityEditor");
            CLASS_ID_FIELD = annotationType!.GetField("classID");
            SCRIPT_CLASS_FIELD = annotationType!.GetField("scriptClass");
            FLAGS_FIELD = annotationType!.GetField("flags");
            ICON_ENABLED_FIELD = annotationType!.GetField("iconEnabled");
            GIZMO_ENABLED_FIELD = annotationType!.GetField("gizmoEnabled");

            var annotationUtility = Type.GetType("UnityEditor.AnnotationUtility, UnityEditor");
            SET_ICON_ENABLED_METHOD = annotationUtility!.GetMethod("SetIconEnabled", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            SET_GIZMO_ENABLED_METHOD = annotationUtility!.GetMethod("SetGizmoEnabled", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
        }

        public bool IconEnabled
        {
            get => ICON_ENABLED_FIELD.TryGetValue<int>(_baseObj, out var value) && (value & 1) == 1;
            set => SET_ICON_ENABLED_METHOD!.Invoke(null, new object[] {ClassID, ScriptClass, value ? 1 : 0});
        }
        public bool GizmoEnabled
        {
            get => GIZMO_ENABLED_FIELD.TryGetValue<int>(_baseObj, out var value) && (value & 1) == 1;
            set => SET_GIZMO_ENABLED_METHOD!.Invoke(null, new object[] {ClassID, ScriptClass, value ? 1 : 0, false});
        }

        public int Flags => _flags ??= FLAGS_FIELD.TryGetValue<int>(_baseObj, out var value) ? value : 0;
        public int ClassID => _classID ??= CLASS_ID_FIELD.TryGetValue<int>(_baseObj, out var value) ? value : 0;
        public string ScriptClass => _scriptClass ??= SCRIPT_CLASS_FIELD.TryGetValue<string>(_baseObj, out var value) ? value : null;

        private int? _flags;
        private int? _classID;
        private string _scriptClass;

        private readonly object _baseObj;

        public AnnotationWrapper(object baseObj)
        {
            _baseObj = baseObj;
        }
    }
}
#endif