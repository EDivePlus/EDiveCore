// Author: František Holubec
// Created: 12.01.2026

#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace EDIVE.BuildTool.Docker
{
    public class CopyDockerfilePostBuild : IPostprocessBuildWithReport
    {
        public int callbackOrder => 10;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.StandaloneLinux64)
                return;

            var buildRoot = Path.GetDirectoryName(report.summary.outputPath);
            if (string.IsNullOrEmpty(buildRoot))
                return;

            var source = Path.Combine(Application.dataPath, "Plugins/Linux/Dockerfile");
            if (!File.Exists(source))
            {
                Debug.LogWarning($"[Docker] No Dockerfile in '{source}' to copy");
                return;
            }

            var destination = Path.Combine(buildRoot, "Dockerfile");

            try
            {
                File.Copy(source, destination, true);
                Debug.Log($"[Docker] Dockerfile copied to build result: {destination}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Docker] Failed to copy Dockerfile: {e}");
            }
        }
    }
}
#endif
