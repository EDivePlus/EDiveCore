// Author: František Holubec
// Created: 21.03.2025

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EDIVE.EditorUtils;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.BuildTool.PathResolving
{
    [Serializable]
    public class BuildPathResolver
    {
        [SerializeField]
        private bool _UseAbsolutePath;

        [Required]
        [ShowIf(nameof(_UseAbsolutePath))]
        [FolderPath(AbsolutePath = true, UseBackslashes = true)]
        [ShowOpenInExplorer]
        [SerializeField]
        private string _AbsoluteRootPath;

        [Required]
        [HideIf(nameof(_UseAbsolutePath))]
        [FolderPath(UseBackslashes = true)]
        [ShowOpenInExplorer]
        [SerializeField]
        private string _RelativeRootPath;
        
        [HideReferenceObjectPicker]
        [SerializeReference]
        [ValueDropdown(nameof(GetSegmentsDropdown), DrawDropdownForListElements = false)]
        private List<ABuildPathSegment> _FolderPathSegments = new();
        
        [HideReferenceObjectPicker]
        [SerializeReference]
        [ValueDropdown(nameof(GetSegmentsDropdown), DrawDropdownForListElements = false)]
        private List<ABuildPathSegment> _FileNameSegments = new();
        
        private string RootPath => _UseAbsolutePath ? _AbsoluteRootPath : _RelativeRootPath;
        
        public FilePath ResolvePath(BuildPreset preset)
        {
            var folderPath = string.Empty;
            var fileName = string.Empty;
            
            var builder = new StringBuilder();
            if (_FolderPathSegments != null)
            {
                foreach (var segment in _FolderPathSegments)
                {
                    builder.Append(segment.GetValue(preset));
                }
                folderPath = Path.Combine(RootPath, builder.ToString());
            }

            builder.Clear();
            if (_FileNameSegments != null)
            {
                foreach (var segment in _FileNameSegments)
                {
                    builder.Append(segment.GetValue(preset));
                }
                builder.Append(preset.PlatformConfig.BuildExtension);
                fileName = builder.ToString();
            }

            return new FilePath(folderPath, fileName);
        }

        private IEnumerable GetSegmentsDropdown() => TypeCacheUtils.GetDerivedClassesOfType<ABuildPathSegment>().Select(s => new ValueDropdownItem<ABuildPathSegment>(s.Label, s));
    }
}
