// Author: František Holubec
// Created: 30.10.2026

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace EDIVE.UnityMCP.Editor
{
    // Lets execute_code use Roslyn from a per-machine folder instead of the project.
    [InitializeOnLoad]
    public static class ExternalRoslynLoader
    {
        public const string VERSION = "4.12.0";

        public static readonly string FOLDER = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UnityRoslyn", VERSION);

        private static readonly string[] ROSLYN = {"Microsoft.CodeAnalysis", "Microsoft.CodeAnalysis.CSharp"};

        // Dependencies are served only once Roslyn was asked for, other assemblies at startup are left alone
        private static bool _roslynRequested;

        static ExternalRoslynLoader()
        {
            if (Directory.Exists(FOLDER))
                AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
        }

        private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            var requested = new AssemblyName(args.Name);
            if (ROSLYN.Contains(requested.Name))
                _roslynRequested = true;
            else if (!_roslynRequested)
                return null;

            var path = Path.Combine(FOLDER, requested.Name + ".dll");
            if (!File.Exists(path))
                return null;

            var loaded = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.GetName().Name == requested.Name)
                .OrderByDescending(a => a.GetName().Version)
                .FirstOrDefault();
            if (loaded != null && (requested.Version == null || loaded.GetName().Version >= requested.Version))
                return loaded;

            return Assembly.Load(File.ReadAllBytes(path));
        }
    }
}
