// Author: František Holubec
// Created: 21.03.2025

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EDIVE.BuildTool.Presets;
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
        [FolderPath(AbsolutePath = true)]
        [ShowOpenInExplorer]
        [SerializeField]
        private string _AbsoluteRootPath;

        [Required]
        [HideIf(nameof(_UseAbsolutePath))]
        [FolderPath]
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

        public bool IsResolved { get; private set; }

        public string FolderPath { get; private set; }
        public string FileName { get; private set; }
        public string FullPath { get; private set; }

        private string RootPath => _UseAbsolutePath ? _AbsoluteRootPath : _RelativeRootPath;

        public void ResolvePath(ABuildPreset preset)
        {
            var builder = new StringBuilder();
            if (_FolderPathSegments != null)
            {
                foreach (var segment in _FolderPathSegments)
                {
                    builder.Append(segment.GetValue(preset));
                }
                FolderPath = Path.Combine(RootPath, builder.ToString());
            }

            builder.Clear();
            if (_FileNameSegments != null)
            {
                foreach (var segment in _FileNameSegments)
                {
                    builder.Append(segment.GetValue(preset));
                }
                builder.Append(preset.BasePlatformConfig.BuildExtension);
                FileName = builder.ToString();
            }

            FullPath = Path.Combine(FolderPath, FileName);
            IsResolved = true;
        }

        private IEnumerable GetSegmentsDropdown() => TypeCacheUtils.GetDerivedClassesOfType<ABuildPathSegment>().Select(s => new ValueDropdownItem<ABuildPathSegment>(s.Label, s));
    }
}
