using System;
using System.Collections.Generic;
using UnityEditor;

namespace EDIVE.EditorUtils
{
    public static class MonoScriptUtility
    {
        private static Dictionary<Type, MonoScript> _monoScripts;
        private static Dictionary<Type, MonoScript> MonoScripts => _monoScripts ??= GetAllMonoScripts();
        
        private static Dictionary<Type, MonoScript> GetAllMonoScripts()
        {
            var allMonoScriptAssets = EditorAssetUtils.FindAllAssetsOfType<MonoScript>(); 
            var resultDictionary = new Dictionary<Type, MonoScript>();
            foreach (var monoScriptAsset in allMonoScriptAssets)
            {
                var representingType = monoScriptAsset.GetClass();
                // This is not UnityEngine.Object type
                if (monoScriptAsset == null || representingType == null) continue;
                
                // May be partial class, this wont work for partial classes
                resultDictionary.TryAdd(representingType, monoScriptAsset);
            }
            return resultDictionary;
        }

        public static bool TryGetMonoScript(this Type targetType, out MonoScript resultMonoScript)
        {
            return MonoScripts.TryGetValue(targetType, out resultMonoScript);
        }
    }
}
