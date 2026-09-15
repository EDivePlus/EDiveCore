// Author: František Holubec
// Created: 15.09.2026

using System;
using System.Collections.Generic;
using System.IO;
using EDIVE.BuildTool.Utils;
using EDIVE.CredentialStore;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace EDIVE.BuildTool.Signing
{
    [Serializable]
    public class AndroidSigningSettings
    {
        public const string PATH_VARIABLE = "KEYSTORE_PATH";
        public const string ALIAS_VARIABLE = "KEYSTORE_ALIAS";
        public const string STORE_PASSWORD_VARIABLE = "KEYSTORE_STOREPASS";
        public const string KEY_PASSWORD_VARIABLE = "KEYSTORE_KEYPASS";

        private const string STORE_PASSWORD_ACCOUNT = "storepass";
        private const string KEY_PASSWORD_PREFIX = "keypass:";

        [PropertyOrder(0)]
        [Sirenix.OdinInspector.FilePath(Extensions = "keystore,jks,ks", AbsolutePath = true)]
        [SerializeField]
        private string _KeystorePath;
        
        [PropertyOrder(2)]
        [EnhancedValueDropdown(nameof(ReadAliases), AppendNextDrawer = true, DontReloadOnInit = true)]
        [SerializeField]
        private string _KeyAlias;

        [PropertyOrder(1)]
        [CredentialField("$CredentialKey", STORE_PASSWORD_ACCOUNT, ValidationMethod = nameof(ValidateStorePassword))]
        [ShowInInspector]
        [NonSerialized]
        private string _storePassword;
        
        [PropertyOrder(3)]
        [CredentialField("$CredentialKey", "$KeyPasswordAccount", ValidationMethod = nameof(ValidateKeyPassword))]
        [ShowInInspector]
        [NonSerialized]
        private string _keyPassword;

        public string KeystorePath => FromEnvironment(PATH_VARIABLE, _KeystorePath);
        public string KeyAlias => FromEnvironment(ALIAS_VARIABLE, _KeyAlias);
        public string CredentialKey => CredentialUtils.SanitizeService(Path.GetFileNameWithoutExtension(KeystorePath));

        private bool IsConfigured => !string.IsNullOrEmpty(_KeystorePath) || !string.IsNullOrEmpty(_KeyAlias);
        private bool HasKeystoreFile => File.Exists(KeystorePath);
        private bool HasAlias => HasKeystoreFile && !string.IsNullOrEmpty(KeyAlias);

        private string KeyPasswordAccount
        {
            get
            {
                var alias = CredentialUtils.SanitizeAccount(KeyAlias);
                return alias == null ? null : KEY_PASSWORD_PREFIX + alias;
            }
        }

        public bool TryResolve(out AndroidSigningData data, out string error)
        {
            data = default;
            if (!HasKeystoreFile)
            {
                error = string.IsNullOrEmpty(KeystorePath)
                    ? "No keystore is set."
                    : $"Keystore '{KeystorePath}' does not exist.";
                return false;
            }

            if (!HasAlias)
            {
                error = "No key alias is set.";
                return false;
            }

            if (!TryGetStorePassword(out var storePassword, out error)
                || !TryGetSecret(KEY_PASSWORD_VARIABLE, KeyPasswordAccount, $"Key password for '{KeyAlias}'", out var keyPassword, out error))
                return false;

            data = new AndroidSigningData(KeystorePath, KeyAlias, storePassword, keyPassword);
            return data.TryVerify(out error);
        }

        public void ApplyIdentity()
        {
            PlayerSettings.Android.keystoreName = KeystorePath ?? string.Empty;
            PlayerSettings.Android.keyaliasName = KeyAlias ?? string.Empty;
        }

        public void LoadCurrent()
        {
            _KeystorePath = PlayerSettings.Android.keystoreName;
            _KeyAlias = PlayerSettings.Android.keyaliasName;
        }

        public bool Validate() => !IsConfigured || HasAlias;

        private bool TryGetStorePassword(out string password, out string error) =>
            TryGetSecret(STORE_PASSWORD_VARIABLE, STORE_PASSWORD_ACCOUNT, "Store password", out password, out error);

        private bool TryGetSecret(string variable, string account, string label, out string secret, out string error)
        {
            error = null;
            secret = Environment.GetEnvironmentVariable(variable);
            if (!string.IsNullOrEmpty(secret))
                return true;

            var missing = $"{label} is not set.";
            var service = CredentialKey;
            if (string.IsNullOrEmpty(service) || string.IsNullOrEmpty(account))
            {
                error = missing;
                return false;
            }

            var result = Credentials.Get(service, account, out secret);
            if (result && !string.IsNullOrEmpty(secret))
                return true;

            // A locked or denied store must not be reported as a missing password.
            secret = null;
            error = result.Status == CredentialStatus.NotFound ? missing : $"{label} could not be read. {result}";
            return false;
        }

        private string ValidateStorePassword(string password)
        {
            return KeystoreAliasReader.TryReadAliases(KeystorePath, password, out _, out var error) ? null : error;
        }

        private string ValidateKeyPassword(string password)
        {
            if (!TryGetStorePassword(out var storePassword, out var error))
                return error;

            return KeystoreAliasReader.VerifyKeyPassword(KeystorePath, storePassword, KeyAlias, password, out error) ? null : error;
        }

        private List<string> ReadAliases()
        {
            return HasKeystoreFile && TryGetStorePassword(out var storePassword, out _)
                ? KeystoreAliasReader.ReadAliasesOrEmpty(KeystorePath, storePassword)
                : new List<string>();
        }

        private static string FromEnvironment(string variable, string fallback)
        {
            var value = Environment.GetEnvironmentVariable(variable);
            return string.IsNullOrEmpty(value) ? fallback : value;
        }
        
    }
}
