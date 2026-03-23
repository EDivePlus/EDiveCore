using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Linq;
using EDIVE.BuildTool.Actions;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace EDIVE.BuildTool.Utils
{
    [Serializable]
    public class BuildResultArchiver : ABuildAction, IPostprocessBuildCallback
    {
        public override int Priority => 9999;
        public override string Tooltip => "Zips the build result folder into a single archive file for easier distribution and storage.";
        
        private static readonly string[] DONT_INCLUDE =
        {
            "_DoNotShip",
            "_ButDontShipItWithYourGame"
        };

        public IEnumerator OnPostprocess(BuildContext context)
        {
            Debug.Log("[BuildResultArchiver] Attempting to zip build...");
            var report = context.Report;
            if (report.summary.result != BuildResult.Succeeded)
                yield break;

            if (!ShouldZip(report))
                yield break;

            Debug.Log("[BuildResultArchiver] Zipping build...");
            var summary = report.summary;
            var files = report.GetFiles();

            var buildFolderPath = Path.GetDirectoryName(summary.outputPath);
            var outputZipPath = Path.ChangeExtension(summary.outputPath, ".zip");

            if (outputZipPath == null || buildFolderPath == null)
            {
                Debug.LogError("[BuildResultArchiver] Zipping build failed! Output path is null.");
                yield break;
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
                
                    if (!CheckIncludePath(filePath))
                        continue;
                
                    var relativePath = Path.GetRelativePath(buildFolderPath, filePath);
                    zip.CreateEntryFromFile(EnsureValidPath(filePath), relativePath, CompressionLevel.Optimal);
                }
                Debug.Log($"[BuildResultArchiver] Zipped build to {outputZipPath}");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError($"[BuildResultArchiver] Zipping build to {outputZipPath} failed with exception.");
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