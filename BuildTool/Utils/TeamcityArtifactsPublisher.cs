// Author: František Holubec
// Created: 25.09.2025

using System;
using System.IO;
using EDIVE.NativeUtils.TeamCity;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using File = UnityEngine.Windows.File;

namespace EDIVE.BuildTool.Utils
{
    [Serializable]
    public class TeamcityArtifactsPublisher : IPostprocessBuildWithReport
    {
        public int callbackOrder => 10000;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.result != BuildResult.Succeeded)
                return;

            if (!TryGetArtifactPath(report, out var artifactPath))
            {
                Debug.LogWarning("[BuildTool] No artifacts to publish.");
                return;
            }

            Debug.Log($"[BuildTool] Publishing artifact: {artifactPath}");
            TeamCityServiceMessages.PublishArtifacts(artifactPath);
        }

        private bool TryGetArtifactPath(BuildReport report, out string path)
        {
            path = report.summary.platform switch
            {
                BuildTarget.Android => report.summary.outputPath,
                BuildTarget.StandaloneWindows or BuildTarget.StandaloneWindows64 or BuildTarget.StandaloneLinux64 => Path.ChangeExtension(report.summary.outputPath, ".zip"),
                _ => null
            };
            return !string.IsNullOrEmpty(path) && File.Exists(path);
        }
    }
}
