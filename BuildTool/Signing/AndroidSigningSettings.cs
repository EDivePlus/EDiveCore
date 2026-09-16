// Author: František Holubec
// Created: 15.09.2026

using System;
using System.Collections.Generic;
using System.IO;
using EDIVE.BuildTool.UserConfigs;
using EDIVE.CredentialStore;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
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

        public const string STORE_PASSWORD_ACCOUNT = "storepass";
        public const string KEY_PASSWORD_PREFIX = "keypass:";
        
        [EnhancedInfoBox("Listed from keystore stored on current user", ButtonAction = nameof(OpenUser), ButtonLabel = "Open User", ButtonIcon = FontAwesomeEditorIconType.UserGearSolid)]
        [PropertyOrder(3)]
        [EnhancedValueDropdown(nameof(ReadAliases), AppendNextDrawer = true, DontReloadOnInit = true)]
        [SerializeField]
        private string _KeyAlias;

        [PropertyOrder(4)]
        [CredentialField("$CredentialKey", "$KeyPasswordAccount", ValidationMethod = nameof(ValidateKeyPassword))]
        [ShowInInspector]
        [NonSerialized]
        private string _keyPassword;

        public string KeystorePath => GetKeystorePath(CurrentUser);
        public string KeyAlias => FromEnvironment(ALIAS_VARIABLE, _KeyAlias);

        private static BuildUserConfig CurrentUser => BuildGlobalSettings.Instance != null ? BuildGlobalSettings.Instance.CurrentUser : null;

        public string CredentialKey => GetCredentialKey(CurrentUser);

        private bool IsConfigured => !string.IsNullOrEmpty(_KeyAlias);
        private bool HasAlias => !string.IsNullOrEmpty(KeyAlias);

        private string KeyPasswordAccount
        {
            get
            {
                var alias = CredentialUtils.SanitizeAccount(KeyAlias);
                return alias == null ? null : KEY_PASSWORD_PREFIX + alias;
            }
        }
        
        public string GetKeystorePath(BuildUserConfig user)
        {
            var fromEnvironment = Environment.GetEnvironmentVariable(PATH_VARIABLE);
            if (!string.IsNullOrEmpty(fromEnvironment))
                return fromEnvironment;

            return user != null && user.TryGetPreference<AppSigningPreference>(out var preference) ? preference.KeystorePath : null;
        }

        public bool TryResolve(out AndroidSigningData data, out string error) => TryResolve(CurrentUser, out data, out error);

        public bool TryResolve(BuildUserConfig user, out AndroidSigningData data, out string error)
        {
            data = default;
            var keystorePath = GetKeystorePath(user);
            if (!File.Exists(keystorePath))
            {
                error = string.IsNullOrEmpty(keystorePath)
                    ? $"No keystore is set for user '{(user != null ? user.name : "none")}'."
                    : $"Keystore '{keystorePath}' does not exist.";
                return false;
            }

            if (!HasAlias)
            {
                error = "No key alias is set.";
                return false;
            }

            if (!TryGetStorePassword(user, out var storePassword, out error)
                || !TryGetSecret(user, KEY_PASSWORD_VARIABLE, KeyPasswordAccount, $"Key password for '{KeyAlias}'", out var keyPassword, out error))
                return false;

            data = new AndroidSigningData(keystorePath, KeyAlias, storePassword, keyPassword);
            return data.TryVerify(out error);
        }

        public void ApplyIdentity()
        {
            PlayerSettings.Android.keystoreName = KeystorePath ?? string.Empty;
            PlayerSettings.Android.keyaliasName = KeyAlias ?? string.Empty;
        }

        public void LoadCurrent()
        {
            _KeyAlias = PlayerSettings.Android.keyaliasName;

            // The path is machine specific, it belongs to the current user, not to this config.
            var user = CurrentUser;
            if (user == null)
                return;

            user.GetOrCreatePreference<AppSigningPreference>().KeystorePath = PlayerSettings.Android.keystoreName;
            EditorUtility.SetDirty(user);
        }

        public bool Validate() => !IsConfigured || HasAlias;

        private string GetCredentialKey(BuildUserConfig user) => CredentialUtils.SanitizeService(Path.GetFileNameWithoutExtension(GetKeystorePath(user)));

        private bool TryGetStorePassword(BuildUserConfig user, out string password, out string error) =>
            TryGetSecret(user, STORE_PASSWORD_VARIABLE, STORE_PASSWORD_ACCOUNT, "Store password", out password, out error);

        private bool TryGetSecret(BuildUserConfig user, string variable, string account, string label, out string secret, out string error)
        {
            error = null;
            secret = Environment.GetEnvironmentVariable(variable);
            if (!string.IsNullOrEmpty(secret))
                return true;

            var missing = $"{label} is not set.";
            var service = GetCredentialKey(user);
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
            if (!TryGetStorePassword(CurrentUser, out var storePassword, out var error))
                return error;

            return KeystoreAliasReader.VerifyKeyPassword(KeystorePath, storePassword, KeyAlias, password, out error) ? null : error;
        }

        private void OpenUser()
        {
            if (CurrentUser == null)
                return;

            OdinEditorWindow.InspectObject(CurrentUser);
        }

        private List<string> ReadAliases()
        {
            var keystorePath = KeystorePath;
            return File.Exists(keystorePath) && TryGetStorePassword(CurrentUser, out var storePassword, out _)
                ? KeystoreAliasReader.ReadAliasesOrEmpty(keystorePath, storePassword)
                : new List<string>();
        }

        private static string FromEnvironment(string variable, string fallback)
        {
            var value = Environment.GetEnvironmentVariable(variable);
            return string.IsNullOrEmpty(value) ? fallback : value;
        }
    }
}
