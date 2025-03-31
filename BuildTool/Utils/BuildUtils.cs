// Author: František Holubec
// Created: 20.03.2025

using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace EDIVE.BuildTool.Utils
{
    public static class BuildUtils
    {
        public const BuildOptions BUILD_OPTIONS = BuildOptions.ShowBuiltPlayer;
        public const BuildOptions BUILD_AND_RUN_OPTIONS = BuildOptions.AutoRunPlayer;
        public const BuildOptions PATCH_OPTIONS = BuildOptions.BuildScriptsOnly | BuildOptions.PatchPackage | BuildOptions.Development;
        public const BuildOptions PATCH_AND_RUN_OPTIONS = PATCH_OPTIONS | BuildOptions.AutoRunPlayer;

        public static readonly string ANDROID_PLAYER_TOOLS_PATH = $"{EditorApplication.applicationContentsPath}/PlaybackEngines/AndroidPlayer";
        public static readonly string OPEN_JDK_BIN_PATH = $"{ANDROID_PLAYER_TOOLS_PATH}/OpenJDK/bin";
        public static readonly string BUNDLE_TOOL_FOLDER = $"{ANDROID_PLAYER_TOOLS_PATH}/Tools/";

        public static readonly string JAVA_EXECUTABLE_PATH = $"\"{OPEN_JDK_BIN_PATH}/java.exe\"";
        public static readonly string JAR_EXECUTABLE_PATH = $"\"{OPEN_JDK_BIN_PATH}/jar.exe\"";

        private const string BUNDLE_TOOL_WILD_CARD = "bundletool-all-*.jar";

        public static NamedBuildTarget CurrentNamedBuildTarget
        {
            get
            {
#if UNITY_SERVER
                return NamedBuildTarget.Server;
#else
                return NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
#endif
            }
        }

        public static bool IsBuildTargetSupported(BuildTarget target)
        {
            var moduleManager = Type.GetType("UnityEditor.Modules.ModuleManager,UnityEditor.dll");
            var isPlatformSupportLoaded = moduleManager!.GetMethod("IsPlatformSupportLoaded", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var getTargetStringFromBuildTarget = moduleManager.GetMethod("GetTargetStringFromBuildTarget", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

            return (bool) isPlatformSupportLoaded!.Invoke(null, new object[] {(string) getTargetStringFromBuildTarget!.Invoke(null, new object[] {target})});
        }

        public static bool TryGetCurrentGitBranch(out string branch)
        {
            branch = null;
            const string processName =
#if UNITY_EDITOR_WIN
                "git.exe";
#elif UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
                "git";
#else
                string.Empty;
#endif

            if (string.IsNullOrEmpty(processName))
                return false;

            try
            {
                var startInfo = new ProcessStartInfo(processName)
                {
                    UseShellExecute = false,
                    WorkingDirectory = Application.dataPath,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    Arguments = "rev-parse --abbrev-ref HEAD"
                };

                var process = new Process();
                process.StartInfo = startInfo;
                process.Start();

                branch = process.StandardOutput.ReadLine();
                return branch != null;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }

        public static void ExtractAAB(string aabPath, bool universalAPK = true, bool openWindow = false)
        {
            var buildPath = Path.GetDirectoryName(aabPath);
            var fileName = Path.GetFileNameWithoutExtension(aabPath);

            var extractCommand = $"{GetExtractApksCommand(aabPath, universalAPK)} & {GetExtractUniversalCommand(buildPath, fileName)}";
            var arguments = $"/k \"{extractCommand}\"";

            var startInfo = new ProcessStartInfo("cmd.exe", arguments);
            if (openWindow)
            {
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
            }
            else
            {
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
            }

            var process = Process.Start(startInfo);
            if (process == null)
                return;

            process.WaitForExit();

            if (!openWindow)
            {
                var standardOutput = process.StandardOutput.ReadToEnd();
                if (!string.IsNullOrWhiteSpace(standardOutput))
                    Debug.Log(standardOutput.Trim());

                var standardError = process.StandardError.ReadToEnd();
                if (!string.IsNullOrWhiteSpace(standardError))
                    Debug.LogError(standardError.Trim());
            }

            process.Close();

            // Update APKs Modified Date, so it can be easily found on NAS
            var apkPath = Path.ChangeExtension(aabPath, "apk");
            if (!File.Exists(apkPath))
                return;
            Debug.Log("APK modified date updated.");

            File.SetLastWriteTime(apkPath, File.GetCreationTime(apkPath));
        }

        private static string GetExtractApksCommand(string aabPath, bool universal = false)
        {
            var buildPath = Path.GetDirectoryName(aabPath);
            var fileName = Path.GetFileNameWithoutExtension(aabPath);

            var signing = GetCurrentSigning();
            var universalBuild = universal ? "--mode=universal" : null;
            var bundleToolPath = GetBundleToolExecutable();
            return $"{JAVA_EXECUTABLE_PATH} -jar {bundleToolPath} build-apks \"{universalBuild}\" --overwrite --bundle=\"{buildPath}/{fileName}.aab\" --output=\"{buildPath}/{fileName}.apks\" {signing}";
        }

        public static string GetBundleToolExecutable()
        {
            var dir = new DirectoryInfo(BUNDLE_TOOL_FOLDER);
            var files = dir.GetFiles(BUNDLE_TOOL_WILD_CARD);
            return files.Length == 0 ? null : $"\"{BUNDLE_TOOL_FOLDER}/{files[0].Name}\"";
        }

        private static string GetCurrentSigning()
        {
            return $"--ks=\"{PlayerSettings.Android.keystoreName}\" --ks-pass=pass:{PlayerSettings.Android.keystorePass} --ks-key-alias={PlayerSettings.Android.keyaliasName} --key-pass=pass:{PlayerSettings.Android.keyaliasPass}";
        }

        private static string GetExtractUniversalCommand(string folderPath, string bundleName)
        {
            return $"cd \"{folderPath}\" & {folderPath[..2]} & {JAR_EXECUTABLE_PATH} -xvf \"{bundleName}.apks\" universal.apk & del /q \"{bundleName}.apk\" & ren universal.apk \"{bundleName}.apk\" & del /q \"{bundleName}.apks\"\"";
        }
    }
}
