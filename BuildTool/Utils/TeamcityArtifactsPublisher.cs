// Author: František Holubec
// Created: 25.09.2025

using System;
using System.Collections;
using System.IO;
using EDIVE.BuildTool.Actions;
using EDIVE.NativeUtils.TeamCity;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using File = UnityEngine.Windows.File;

namespace EDIVE.BuildTool.Utils
{
    [Serializable]
    public class TeamcityArtifactsPublisher : ABuildAction, IPostprocessBuildCallback
    {
        public override int Priority => 10000;
        public override string Tooltip => "Publishes build artifacts to TeamCity server using TeamCity service messages.";

        public IEnumerator OnPostprocess(BuildContext context)
        {
            Debug.Log("[TeamcityArtifactsPublisher] Attempting to publish artifacts to TeamCity...");
            
            var report = context.Report;
            if (report.summary.result != BuildResult.Succeeded)
                yield break;

            if (!TryGetArtifactPath(report, out var artifactPath))
            {
                Debug.LogWarning("[TeamcityArtifactsPublisher] No artifacts to publish.");
                yield break;
            }

            Debug.Log($"[TeamcityArtifactsPublisher] Publishing artifact: {artifactPath}");
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
