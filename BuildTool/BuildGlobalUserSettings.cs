// Author: František Holubec
// Created: 21.03.2025

using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using FilePathAttribute = UnityEditor.FilePathAttribute;

namespace EDIVE.BuildTool
{
    [FilePath("UserSettings/BuildSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class BuildGlobalUserSettings : ScriptableSingleton<BuildGlobalUserSettings>
    {
        [HideInInspector]
        [SerializeField]
        private BuildUserConfig _CurrentUser;

        [ShowInInspector]
        public BuildUserConfig CurrentUser
        {
            get => _CurrentUser;
            set
            {
                _CurrentUser = value;
                SaveFile();
            }
        }

        public void SaveFile() => Save(true);
    }
}
