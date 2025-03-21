using System.IO;
using UnityEngine;

namespace EDIVE.NativeUtils
{
    public static class PathUtility
    {
        public static string GetAbsolutePath(string projectPath)
        {
            return $"{Application.dataPath[..(Application.dataPath.LastIndexOf('/') + 1)]}{projectPath}";
        }
        
        public static string GetProjectRelativePath(string absolutePath)
        {
            if (absolutePath.Replace("\\","/").StartsWith(Application.dataPath))
                return "Assets" + absolutePath[Application.dataPath.Length..];
            return string.Empty;
        }
        
        public static string GetFullPathWithoutExtension(string path)
        {
            return Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, Path.GetFileNameWithoutExtension(path));
        }
        
        public static string ConvertPathSeparator(this string path)
        {
            return path.Replace('\\', '/');
        }

        public static void EnsurePathExists(string filePath)
        {
            var dirPath = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dirPath))
                Directory.CreateDirectory(dirPath);
        }
    }
}