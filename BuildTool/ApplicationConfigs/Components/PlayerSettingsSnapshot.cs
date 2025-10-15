// Author: František Holubec
// Created: 15.10.2025

using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace EDIVE.BuildTool.ApplicationConfigs.Components
{
    [Serializable]
    public class PlayerSettingsSnapshot : AApplicationConfigComponent
    {
        [Required]
        [SerializeField]
        private string _ProductName;
        
        [Required]
        [SerializeField]
        private string _PackageName;
        
        [Required]
        [SerializeField]
        [EnhancedPreviewField]
        private Texture2D _DefaultIcon;

        public string ProductName => _ProductName;
        public string PackageName => _PackageName;

        private const string ANDROID_RESOLVER_DEPENDENCIES_FILEPATH = "ProjectSettings/AndroidResolverDependencies.xml";
        
        public override IEnumerator Apply()
        {
            PlayerSettings.productName = ProductName;
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown,new[] { _DefaultIcon }, IconKind.Any);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageName);

            if (File.Exists(ANDROID_RESOLVER_DEPENDENCIES_FILEPATH))
            {
                var config = File.ReadAllText(ANDROID_RESOLVER_DEPENDENCIES_FILEPATH);
                var xDocument = XDocument.Parse(config);
                xDocument.Element("dependencies")
                    ?.Element("settings")
                    ?.Elements().FirstOrDefault(e => e.Attribute("name")?.Value == "bundleId")
                    ?.SetAttributeValue("value", PackageName);
                
                File.WriteAllText(ANDROID_RESOLVER_DEPENDENCIES_FILEPATH, xDocument.ToString());
            }
            AssetDatabase.SaveAssets();
            yield break;
        }

        public override IEnumerator LoadCurrent()
        {
            _ProductName = PlayerSettings.productName;
            _PackageName = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            _DefaultIcon = PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Any).FirstOrDefault();
            yield break;
        }

        public override bool Validate()
        {
            return !string.IsNullOrEmpty(ProductName) && !string.IsNullOrEmpty(PackageName);
        }
    }
}
