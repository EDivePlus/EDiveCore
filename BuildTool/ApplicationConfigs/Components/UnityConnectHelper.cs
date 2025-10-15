// Author: František Holubec
// Created: 15.10.2025

#if UNITY_EDITOR
using System;
using UnityEngine;

namespace EDIVE.BuildTool.ApplicationConfigs.Components
{
    public static class UnityConnectHelper
    {
        private static object _settingsInstanceBackingField;
        private static Type _settingsTypeBackingField;
        private static Type SettingsType => _settingsTypeBackingField ??= Type.GetType("UnityEditor.Connect.UnityConnect, UnityEditor, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null");

        private static object SettingsInstance
        {
            get 
            {
                if (_settingsInstanceBackingField != null) return _settingsInstanceBackingField;
                var instanceInfo = SettingsType.GetProperty("instance");
                _settingsInstanceBackingField = instanceInfo?.GetValue(null, null);

                return _settingsInstanceBackingField;
            }
        }

        private static void InvokeVoidMethod(object instance, Type type, string methodName, object[] parameters)
        {
            var method = type.GetMethod(methodName);
            method?.Invoke(instance, parameters);
        }

        private static void InvokeSettingsMethod(string methodName, object[] parameters)
        {
            InvokeVoidMethod(SettingsInstance, SettingsType, methodName, parameters);
        }

        private static void InvokeSettingsMethod(string methodName)
        {
            InvokeSettingsMethod(methodName, new object[] { });
        }

        public static void BindProject(string projectGuid, string projectName, string organizationId)
        {
            Debug.Log($"Binding cloud project to {projectGuid}: {projectName} ({organizationId})");
            InvokeSettingsMethod("BindProject", new object[] {projectGuid, projectName, organizationId});
        }

        public static void UnbindProject()
        {
            Debug.Log("Unbinding current cloud project");
            InvokeSettingsMethod("UnbindProject");
        }
    }
}
#endif