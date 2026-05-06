// Author: František Holubec
// Created: 06.03.2026

#if UNITY_6000_3_OR_NEWER
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace EDIVE.EditorUtils
{
    public static class MainToolbarUtility
    {
        private const string ROOT_ELEMENT_PATH = "EDive";
        private const string ENABLED_FLAG_PREFIX = "EDIVE.MainToolbarElements.Initialized";

        private static readonly ConstructorInfo CTOR;
        private static readonly MethodInfo METHOD_SET_DISPLAYED_ALL;

        static MainToolbarUtility()
        {
            var customType = Type.GetType("UnityEditor.Toolbars.MainToolbarCustom, UnityEditor");
            CTOR = customType?.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new[] { typeof(Func<VisualElement>) }, null);

            var mainToolbarType = Type.GetType("UnityEditor.Toolbars.MainToolbar, UnityEditor");
            METHOD_SET_DISPLAYED_ALL = mainToolbarType?.GetMethod("SetDisplayedAll", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        public static MainToolbarElement CreateElement(Func<VisualElement> createElement)
        {
            return CTOR == null ? null : CTOR.Invoke(new object[] { createElement }) as MainToolbarElement;
        }

        public static void SetDisplayAll(string id, bool state)
        {
            METHOD_SET_DISPLAYED_ALL.Invoke(null, new object[] { id, state });
        }

        [InitializeOnLoadMethod]
        private static void EnableEDiveElementsOnFirstRun()
        {
            var flagKey = $"{ENABLED_FLAG_PREFIX}.{PlayerSettings.productGUID}";
            if (EditorPrefs.GetBool(flagKey, false) || METHOD_SET_DISPLAYED_ALL == null) 
                return;
            
            EditorApplication.delayCall += () =>
            {
                try
                {
                    // Disable annoying meta bullshit
                    SetDisplayAll("MetaXR", false);
                    EditorPrefs.SetBool("Meta.XR.SDK.StatusIcon.Enabled", false);
                    
                    // Enable all EDive elements
                    SetDisplayAll(ROOT_ELEMENT_PATH, true);
                    EditorPrefs.SetBool(flagKey, true);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            };
        }
    }
}
#endif
