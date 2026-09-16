// Author: František Holubec
// Created: 16.09.2026

using System;
using System.IO;
using EDIVE.BuildTool.UserConfigs;
using EDIVE.CredentialStore;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.BuildTool.Signing
{
    [Serializable]
    public class AppSigningPreference : AUserPreference
    {
        [EnhancedBoxGroup("Android", "@ColorTools.Green")]
        [FilePath(Extensions = "keystore,jks,ks", AbsolutePath = true)]
        [ShowOpenInExplorer]
        [SerializeField]
        private string _KeystorePath;
        
        [EnhancedBoxGroup("Android")]
        [PropertyOrder(1)]
        [CredentialField("$CredentialKey", AndroidSigningSettings.STORE_PASSWORD_ACCOUNT, ValidationMethod = nameof(ValidateStorePassword))]
        [ShowInInspector]
        [NonSerialized]
        private string _storePassword;

        public override string Label => "App Signing";

        public string KeystorePath
        {
            get => _KeystorePath; 
            set => _KeystorePath = value;
        }
        
        public string CredentialKey => CredentialUtils.SanitizeService(Path.GetFileNameWithoutExtension(_KeystorePath));
        
        private string ValidateStorePassword(string password)
        {
            return KeystoreAliasReader.TryReadAliases(KeystorePath, password, out _, out var error) ? null : error;
        }
    }
}
