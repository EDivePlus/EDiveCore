#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace EDIVE.EditorUtils.AnnotationUtility
{
    public static class AnnotationUtilityHelper
    {
        private static readonly MethodInfo GET_ANNOTATIONS_METHOD;

        static AnnotationUtilityHelper()
        {
            var annotationUtility = Type.GetType("UnityEditor.AnnotationUtility, UnityEditor");
            GET_ANNOTATIONS_METHOD = annotationUtility!.GetMethod("GetAnnotations", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
        }

        public static IEnumerable<AnnotationWrapper> GetAnnotations()
        {
            return ((Array) GET_ANNOTATIONS_METHOD!.Invoke(null, null)).Cast<object>().Select(o => new AnnotationWrapper(o));
        }

        [MenuItem("Tools/Disable All Scene Icons", priority = 200)]
        public static void DisableAllSceneIcons()
        {
            var annotations = GetAnnotations();
            foreach (var annotation in annotations)
            {
                annotation.IconEnabled = false;
            }
        }
    }
}
#endif