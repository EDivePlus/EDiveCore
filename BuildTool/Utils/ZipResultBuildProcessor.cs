using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace EDIVE.BuildTool.Utils
{
    public class ZipResultBuildProcessor : IPostprocessBuildWithReport
    {
        public int callbackOrder => 9999;
        
        private static readonly string[] DONT_INCLUDE =
        {
            "_DoNotShip",
            "_ButDontShipItWithYourGame"
        };

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.result != BuildResult.Succeeded)
                return;

            if (!ShouldZip(report))
                return;

            Debug.Log("[BuildTool] Zipping build...");
            var summary = report.summary;
            var files = report.GetFiles();

            var buildFolderPath = Path.GetDirectoryName(summary.outputPath);
            var outputZipPath = Path.ChangeExtension(summary.outputPath, ".zip");

            if (outputZipPath == null || buildFolderPath == null)
            {
                Debug.LogError("[BuildTool] Zipping build failed! Output path is null.");
                return;
            }

            if (File.Exists(outputZipPath))
                File.Delete(outputZipPath);

            try
            {
                using var zip = ZipFile.Open(outputZipPath, ZipArchiveMode.Create);
                foreach (var file in files)
                {
                    var filePath = file.path;
                    var fileName = Path.GetFileName(filePath);
                
                    if (!CheckIncludePath(fileName))
                        continue;
                
                    var relativePath = Path.GetRelativePath(buildFolderPath, filePath);
                    zip.CreateEntryFromFile(EnsureValidPath(filePath), relativePath, CompressionLevel.Optimal);
                }
                Debug.Log($"[BuildTool] Zipped build to {outputZipPath}");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError($"[BuildTool] Zipping build to {outputZipPath} failed with exception.");
            }
        }

        private static bool ShouldZip(BuildReport report)
        {
            return report.summary.platform is BuildTarget.StandaloneWindows or BuildTarget.StandaloneWindows64 or BuildTarget.StandaloneLinux64;
        }

        private static string EnsureValidPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;
            
            var fullPath = Path.GetFullPath(path);
            
            // Windows MAX_PATH limitation workaround
            if (fullPath.Length >= 260 && !fullPath.StartsWith(@"\\?\"))
                return @"\\?\" + fullPath;

            return fullPath;
        }
        
        private static bool CheckIncludePath(string path)
        {
            return DONT_INCLUDE.All(exclude => !path.Contains(exclude));
        }
    }
}