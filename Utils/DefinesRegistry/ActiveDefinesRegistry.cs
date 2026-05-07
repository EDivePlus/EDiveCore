// Author: František Holubec
// Created: 07.05.2026

using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor.Compilation;
#endif

namespace EDIVE.Utils.DefinesRegistry
{
    public static class ActiveDefinesRegistry
    {
        public const string RESOURCE_PATH = "ActiveDefines";

        private static HashSet<string> _defines;
        private static HashSet<string> Defines => _defines ??= LoadDefines();

        public static bool IsDefined(string define) => !string.IsNullOrEmpty(define) && Defines.Contains(define);

        private static HashSet<string> LoadDefines()
        {
#if UNITY_EDITOR
            var assemblies = CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies);
            return new HashSet<string>(assemblies.SelectMany(a => a.defines ?? Array.Empty<string>()));
#else
            var asset = Resources.Load<ActiveDefinesAsset>(RESOURCE_PATH);
            return asset != null ? new HashSet<string>(asset.Defines) : new HashSet<string>();
#endif
        }

#if UNITY_EDITOR
        public static void Invalidate() => _defines = null;
#endif
    }
}
