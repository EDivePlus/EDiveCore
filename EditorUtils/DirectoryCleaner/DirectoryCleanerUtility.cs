using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EDIVE.NativeUtils;
using UnityEditor;
using UnityEngine;

namespace EDIVE.EditorUtils.DirectoryCleaner
{
    public static class DirectoryCleanerUtility
    {
        private const string CLEAR_ON_SAVE_PREFS_KEY = "DirectoryCleanerUtility_ClearOnSave";

        public static bool CleanOnEditorInitialized
        {
            get => EditorPrefs.GetBool(CLEAR_ON_SAVE_PREFS_KEY, false); 
            set => EditorPrefs.SetBool(CLEAR_ON_SAVE_PREFS_KEY, value);
        }

        [InitializeOnLoadMethod]
        private static void OnEditorLoaded()
        {
            if (!CleanOnEditorInitialized) return;

            var emptyDirs = GetEmptyDirectories();
            if (!emptyDirs.Any()) 
                return;
            
            DeleteDirectories(emptyDirs);
            Debug.Log( "[DirectoryCleaner] Cleared Empty Directories" );
        }

        public static void DeleteDirectories(IEnumerable<DirectoryInfo> directories)
        {
            var failedPaths = new List<string>();
            AssetDatabase.MoveAssetsToTrash(directories.Select(d => PathUtility.GetProjectRelativePath(d.FullName)).ToArray(), failedPaths);
        }
        
        public static void DeleteDirectory(DirectoryInfo directory)
        {
            AssetDatabase.MoveAssetToTrash(PathUtility.GetProjectRelativePath(directory.FullName));
        }

        public static List<DirectoryInfo> GetEmptyDirectories()
        {
            var emptyDirs = new List<DirectoryInfo>();
            var assetsDir = new DirectoryInfo(Application.dataPath);
            CheckEmptyDirectory(assetsDir, ref emptyDirs);
            return emptyDirs;
        }

        private static bool CheckEmptyDirectory(DirectoryInfo root, ref List<DirectoryInfo> emptyDirs)
        {
            try
            {
                var subDirs = root.GetDirectories();
                var allSubDirsEmpty = true;
                foreach (var subDir in subDirs)
                {
                    if (!CheckEmptyDirectory(subDir, ref emptyDirs))
                    {
                        allSubDirsEmpty = false;
                    }
                }

                if (!allSubDirsEmpty || HasDirectoryAnyFiles(root))
                    return false;
            
                if(!emptyDirs.Contains(root))
                    emptyDirs.Add(root);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool HasDirectoryAnyFiles(DirectoryInfo dirInfo)
        {
            try
            {
                return dirInfo.EnumerateFileSystemInfos("*.*").Any(IsNonMetaFile);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool IsNonMetaFile(FileSystemInfo file)
        {
            return file.Extension != ".meta";
        }
    }
}
