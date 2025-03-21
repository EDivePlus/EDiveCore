// Author: František Holubec
// Created: 21.03.2025

using System;
using System.Collections.Generic;
using System.Text;
using EDIVE.BuildTool.Presets;
using UnityEngine;

namespace EDIVE.BuildTool.PathResolving
{
    [Serializable]
    public class BuildPathResolver
    {
        [SerializeField]
        private string _RootPath;

        [SerializeReference]
        private List<ABuildPathSegment> _FolderPathSegments;

        [SerializeReference]
        private List<ABuildPathSegment> _FileNameSegments;

        public string FolderPath { get; private set; }
        public string FileName { get; private set; }
        public string FullPath { get; private set; }

        public void ResolvePath(ABuildPreset preset)
        {
            FolderPath = _RootPath;
            FileName = string.Empty;
            FullPath = string.Empty;

            var builder = new StringBuilder();
            if (_FolderPathSegments != null)
            {
                foreach (var segment in _FolderPathSegments)
                {
                    builder.Append(segment.GetValue(preset));
                }
            }

            builder.Clear();
            if (_FileNameSegments != null)
            {
                foreach (var segment in _FileNameSegments)
                {
                    builder.Append(segment.GetValue(preset));
                }
                builder.Append(preset.BasePlatformConfig.BuildExtension);
            }

            FullPath = System.IO.Path.Combine(FolderPath, FileName);
        }
    }
}
