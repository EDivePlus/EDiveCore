// Author: František Holubec
// Created: 08.12.2025

#if UNITY_EDITOR
using System.Reflection;
using EDIVE.NativeUtils;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

namespace EDIVE.External.OpenXR
{
    public class OpenXRValidationRuleFixer : IPreprocessBuildWithReport
    {
        public int callbackOrder => -100;

        public void OnPreprocessBuild(BuildReport report)
        {
            var field = typeof(OpenXRProjectValidation).GetField("BuiltinValidationRules", BindingFlags.NonPublic | BindingFlags.Static);
            if (field == null || field.GetValue(null) is not OpenXRFeature.ValidationRule[] rules || rules.Length == 0) 
                return;
            
            if (rules.TryGetFirst(r => r.message != null && r.message.Contains("The only standalone targets supported are Windows x64 and OSX with OpenXR"), out var rule))
            {
                rule.checkPredicate = () => true;
            }
            field.SetValue(null, rules);
        }
    }
}
#endif
