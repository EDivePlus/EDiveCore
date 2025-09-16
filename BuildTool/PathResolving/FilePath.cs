// Author: František Holubec
// Created: 16.09.2025

using System;
using UnityEngine;

namespace EDIVE.BuildTool.PathResolving
{
    [Serializable]
    public class FilePath
    {
        [SerializeField]
        private string _FolderPath;
        
        [SerializeField]
        private string _FileName;
        
        public string FolderPath => _FolderPath;
        public string FileName => _FileName;
        public string FullPath => System.IO.Path.Combine(FolderPath, FileName);
        
        public FilePath(string folderPath, string fileName)
        {
            _FolderPath = folderPath;
            _FileName = fileName;
        }
    }
}
