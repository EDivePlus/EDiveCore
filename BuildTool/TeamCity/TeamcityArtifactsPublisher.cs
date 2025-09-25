// Author: František Holubec
// Created: 25.09.2025

using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Linq;
using EDIVE.BuildTool.Actions;
using EDIVE.NativeUtils.TeamCity;
using UnityEditor;
using UnityEditor.Build.Reporting;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace EDIVE.BuildTool.TeamCity
{
    [Serializable]
    public class TeamcityArtifactsPublisher : ABuildAction
    {
        private static readonly string[] WINDOWS_DONT_INCLUDE = {"DoNotShip", "DontShip"};
        
        public override IEnumerator OnPostprocess(BuildContext buildContext)
        {
            if (buildContext.Result != BuildResult.Succeeded)
                yield break;
            
            if (!buildContext.ResultPath.IsValid)
                yield break;

            if (buildContext.PlatformConfig.BuildTarget is BuildTarget.Android)
            {
                TeamCityServiceMessages.PublishArtifacts(buildContext.ResultPath.FullPath); 
            }

            if (buildContext.PlatformConfig.BuildTarget is BuildTarget.StandaloneWindows or BuildTarget.StandaloneWindows64)
            {
                if (TryZipWindowsBuild(buildContext, out var outputZipPath))
                    TeamCityServiceMessages.PublishArtifacts(outputZipPath);
            }
        }

        public static bool TryZipWindowsBuild(BuildContext buildContext, out string outputZipPath)
        {
            outputZipPath = null;
            var buildPath = buildContext.ResultPath.FolderPath;
            if (string.IsNullOrEmpty(buildPath))
                return false;
            
            outputZipPath = $"{buildPath}.zip";
            
            if (File.Exists(outputZipPath))
                File.Delete(outputZipPath);

            using var zip = ZipFile.Open(outputZipPath, ZipArchiveMode.Create);
            foreach (var file in Directory.GetFiles(buildPath))
            {
                var fileName = Path.GetFileName(file);
                if (!CheckIncludePath(fileName)) 
                    continue;
                
                zip.CreateEntryFromFile(file, fileName, CompressionLevel.Optimal);
            }

            foreach (var dir in Directory.GetDirectories(buildPath))
            {
                var dirName = Path.GetFileName(dir);
                if (!CheckIncludePath(dirName)) 
                    continue;
                
                foreach (var file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
                {
                    var relativePath = Path.Combine(dirName, Path.GetRelativePath(dir, file));
                    zip.CreateEntryFromFile(file, relativePath, CompressionLevel.Optimal);
                }
            }

            return true;
        }

        private static bool CheckIncludePath(string path)
        {
            return WINDOWS_DONT_INCLUDE.All(exclude => !path.Contains(exclude));
        }

        public override int Priority => 10000;
    }
}
