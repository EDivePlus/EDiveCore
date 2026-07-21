// Author: František Holubec
// Created: 13.09.2025

using System;
using System.Collections.Generic;
using EDIVE.BuildTool.ApplicationConfigs;
using EDIVE.BuildTool.PlatformConfigs;
using EDIVE.EditorUtils;
using EDIVE.NativeUtils;
using EDIVE.NativeUtils.TeamCity;
using JetBrains.Annotations;
using UnityEditor;

namespace EDIVE.BuildTool
{
    public class CommandLineBuild
    {
        private const string CMD_USER_CONFIG = "-userConfig";
        private const string CMD_APP_CONFIG = "-appConfig";
        private const string CMD_PLATFORM_CONFIG = "-platformConfig";

        private const string CMD_VERSION_MAJOR = "-vMaj";
        private const string CMD_VERSION_MINOR = "-vMin";
        private const string CMD_VERSION_PATCH = "-vPatch";
        private const string CMD_VERSION_BUILD = "-vBuild";

        private const char CMD_PREFIX = '-';

        [UsedImplicitly]
        public static void Build()
        {
            TeamCityServiceMessages.MessageLog("[CMDBuild] Initializing command line build...");

            var arguments = GetArguments();
            if (!arguments.TryGetValue(CMD_PLATFORM_CONFIG, out var platformConfigName))
            {
                TeamCityServiceMessages.MessageBuildProblem("[CMDBuild] Platform config not specified");
                return;
            }
            
            if (!EditorAssetUtils.FindAllAssetsOfType<BuildPlatformConfig>().TryGetFirst(c => c.name == platformConfigName, out var platformConfig ))
            {
                TeamCityServiceMessages.MessageBuildProblem($"[CMDBuild] Platform config '{platformConfigName}' not found");
                return;
            }
        
            if (!platformConfig.IsValid)
            {
                TeamCityServiceMessages.MessageBuildProblem($"[CMDBuild] Platform config '{platformConfigName}' is not valid!, open project and fix the issues.");
                return;
            }
            
            var userConfig = BuildGlobalSettings.Instance.DefaultUser;
            if (arguments.TryGetValue(CMD_USER_CONFIG, out var userName))
            {
                if (EditorAssetUtils.FindAllAssetsOfType<BuildUserConfig>().TryGetFirst(c => c.name == userName, out var foundUser))
                    userConfig = foundUser;
                else
                    TeamCityServiceMessages.MessageBuildProblem($"[CMDBuild] User config '{userName}' not found, using default.");
            }
            
            ApplicationConfig appConfig = null;
            if (arguments.TryGetValue(CMD_APP_CONFIG, out var appConfigName))
            {
                if (EditorAssetUtils.FindAllAssetsOfType<ApplicationConfig>().TryGetFirst(c => c.name == appConfigName, out var foundApp))
                    appConfig = foundApp;
                else
                    TeamCityServiceMessages.MessageBuildProblem($"[CMDBuild] Application config '{appConfigName}' not found, using default.");
            }
            
            var versionDef = BuildGlobalSettings.Instance.VersionDefinition;
            var version = versionDef.CurrentVersion;
            if (arguments.TryGetValue(CMD_VERSION_MAJOR, out var vMajStr) && int.TryParse(vMajStr, out var vMaj))
                version.Major = vMaj;
            if (arguments.TryGetValue(CMD_VERSION_MINOR, out var vMinStr) && int.TryParse(vMinStr, out var vMin))
                version.Minor = vMin;
            if (arguments.TryGetValue(CMD_VERSION_PATCH, out var vPatchStr) && int.TryParse(vPatchStr, out var vPatch))
                version.Patch = vPatch;
            if (arguments.TryGetValue(CMD_VERSION_BUILD, out var vBuildStr) && int.TryParse(vBuildStr, out var vBuild))
                version.Build = vBuild;
            versionDef.CurrentVersion = version;

            var preset = new BuildPreset(userConfig, appConfig, platformConfig);
        
            TeamCityServiceMessages.MessageLog($"[CMDBuild] Preset created: {preset}");
            preset.Build(BuildOptions.None);
        }

        private static Dictionary<string, string> GetArguments()
        {
            var commandToValueDictionary = new Dictionary<string, string>();
            var args = Environment.GetCommandLineArgs();

            for (var i = 0; i < args.Length; i++)
            {
                if (!args[i].StartsWith(CMD_PREFIX.ToString())) continue;
                var command = args[i];
                var value = string.Empty;

                if (i < args.Length - 1 && !args[i + 1].StartsWith(CMD_PREFIX.ToString()))
                {
                    value = args[i + 1];
                    i++;
                }

                if (!commandToValueDictionary.TryAdd(command, value))
                    TeamCityServiceMessages.MessageLog($"[CMDBuild] Duplicate command line argument: '{command}'", ServiceMessageLogType.Warning);
            }

            return commandToValueDictionary;
        }
    }
}
