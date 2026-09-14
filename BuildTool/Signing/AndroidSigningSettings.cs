// Author: František Holubec
// Created: 15.09.2026

using System;
using System.Collections.Generic;
using System.IO;
using EDIVE.BuildTool.Utils;
using EDIVE.CredentialStore;
using EDIVE.OdinExtensions;
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

        // One store password per keystore, one key password per alias, so aliases never duplicate it.
        private const string STORE_PASSWORD_ACCOUNT = "storepass";
        private const string KEY_PASSWORD_PREFIX = "keypass:";

        [PropertyOrder(0)]
        [Sirenix.OdinInspector.FilePath(Extensions = "keystore,jks,ks", AbsolutePath = true)]
        [OnValueChanged(nameof(InvalidateKeystore))]
        [SerializeField]
        private string _KeystorePath;

        // Typed passwords are never serialized, they only live until they are saved to the Credential Store.
        [NonSerialized]
        private string _storePassword;

        [NonSerialized]
        private string _keyPassword;

        [PropertyOrder(1)]
        [LabelText("Store Password")]
        [CredentialField("$CredentialKey", STORE_PASSWORD_ACCOUNT, ValidationMethod = nameof(ValidateStorePassword))]
        [ShowInInspector]
        private string StorePassword
        {
            get => _storePassword;
            set => _storePassword = value;
        }

        [PropertyOrder(2)]
        [InfoBox("$_aliasError", InfoMessageType.Warning, VisibleIf = "@!string.IsNullOrEmpty(_aliasError)")]
        [EnhancedValueDropdown(nameof(GetAliases), AppendNextDrawer = true)]
        [OnValueChanged(nameof(InvalidateAlias))]
        [InlineIconButton(FontAwesomeEditorIconType.ArrowsRotateSolid, nameof(RefreshAliases), "Read the aliases from the keystore", EnableIf = nameof(HasKeystoreFile))]
        [SerializeField]
        private string _KeyAlias;

        [PropertyOrder(3)]
        [LabelText("Key Password")]
        [CredentialField("$CredentialKey", "$KeyPasswordAccount", ValidationMethod = nameof(ValidateKeyPassword))]
        [ShowInInspector]
        private string KeyPassword
        {
            get => _keyPassword;
            set => _keyPassword = value;
        }

        [NonSerialized]
        private List<string> _aliasCache;

        [NonSerialized]
        private string _aliasError;

        public string KeystorePath => FromEnvironment(PATH_VARIABLE, _KeystorePath);
        public string KeyAlias => FromEnvironment(ALIAS_VARIABLE, _KeyAlias);
        public string CredentialKey => ServiceFor(KeystorePath);

        public bool TryResolve(out AndroidSigningData data, out string error)
        {
            data = default;
            var keystorePath = KeystorePath;
            var keyAlias = KeyAlias;

            if (string.IsNullOrEmpty(keystorePath))
            {
                error = $"No keystore is set. Set it in the application config or ${PATH_VARIABLE}.";
                return false;
            }

            if (!File.Exists(keystorePath))
            {
                error = $"Keystore '{keystorePath}' does not exist.";
                return false;
            }

            if (string.IsNullOrEmpty(keyAlias))
            {
                error = $"No key alias is set. Set it in the application config or ${ALIAS_VARIABLE}.";
                return false;
            }

            if (!TryGetStorePassword(out var storePassword, out error))
                return false;

            if (!TryGetKeyPassword(out var keyPassword, out error))
                return false;

            data = new AndroidSigningData(keystorePath, keyAlias, storePassword, keyPassword);
            error = null;
            return true;
        }

        public static bool TrySavePasswords(string keystorePath, string alias, string storePassword, string keyPassword, out string error)
        {
            return TrySaveSecret(keystorePath, STORE_PASSWORD_ACCOUNT, storePassword, out error)
                && TrySaveSecret(keystorePath, KeyAccountFor(alias), keyPassword, out error);
        }

        public void Invalidate() => InvalidateKeystore();

        public void ApplyIdentity()
        {
            PlayerSettings.Android.keystoreName = KeystorePath ?? string.Empty;
            PlayerSettings.Android.keyaliasName = KeyAlias ?? string.Empty;
        }

        public void LoadCurrent()
        {
            _KeystorePath = PlayerSettings.Android.keystoreName;
            _KeyAlias = PlayerSettings.Android.keyaliasName;
            InvalidateKeystore();
        }

        public bool Validate() => !IsConfigured || (HasKeystoreFile && HasAlias);

        private bool IsConfigured => !string.IsNullOrEmpty(_KeystorePath) || !string.IsNullOrEmpty(_KeyAlias);

        private bool HasKeystoreFile => !string.IsNullOrEmpty(KeystorePath) && File.Exists(KeystorePath);

        private bool HasAlias => HasKeystoreFile && !string.IsNullOrEmpty(KeyAlias);

        private string KeyPasswordAccount => KeyAccountFor(KeyAlias);

        private static string ServiceFor(string keystorePath) =>
            CredentialUtils.SanitizeService(Path.GetFileNameWithoutExtension(keystorePath));

        private static string KeyAccountFor(string alias)
        {
            var sanitized = CredentialUtils.SanitizeAccount(alias);
            return string.IsNullOrEmpty(sanitized) ? null : KEY_PASSWORD_PREFIX + sanitized;
        }

        #region Secrets

        // A build only ever uses the environment or the Credential Store. The typed field is included
        // for the inspector alone, so a password can be tried before it is saved without a half typed
        // one leaking into a build or overriding a password that is already saved and correct.
        private bool TryGetStorePassword(out string password, out string error, bool includeTyped = false) =>
            TryGetSecret(STORE_PASSWORD_VARIABLE, STORE_PASSWORD_ACCOUNT, "Store password", includeTyped ? _storePassword : null, out password, out error);

        private bool TryGetKeyPassword(out string password, out string error, bool includeTyped = false) =>
            TryGetSecret(KEY_PASSWORD_VARIABLE, KeyPasswordAccount, $"Key password for '{KeyAlias}'", includeTyped ? _keyPassword : null, out password, out error);

        private bool TryGetSecret(string variable, string account, string label, string typed, out string secret, out string error)
        {
            error = null;

            secret = Environment.GetEnvironmentVariable(variable);
            if (!string.IsNullOrEmpty(secret))
                return true;

            if (!string.IsNullOrEmpty(typed))
            {
                secret = typed;
                return true;
            }

            var service = CredentialKey;
            var missing = $"{label} is not set. Enter it in the application config or set ${variable}.";
            if (string.IsNullOrEmpty(service) || string.IsNullOrEmpty(account))
            {
                secret = null;
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

        private static bool TrySaveSecret(string keystorePath, string account, string secret, out string error)
        {
            error = null;
            var service = ServiceFor(keystorePath);
            if (string.IsNullOrEmpty(service))
            {
                error = "No keystore is set.";
                return false;
            }
            if (string.IsNullOrEmpty(account))
            {
                error = "No key alias is set.";
                return false;
            }
            if (string.IsNullOrEmpty(secret))
            {
                error = "Password is empty.";
                return false;
            }

            var result = Credentials.Set(service, account, secret);
            error = result ? null : result.ToString();
            return result;
        }

        private string ValidateStorePassword(string password)
        {
            if (!KeystoreAliasReader.TryReadAliases(KeystorePath, password, out var aliases, out var error))
                return error;

            // The aliases were just read with this password, so the dropdown fills straight away.
            _aliasCache = aliases;
            _aliasError = null;
            return null;
        }

        private string ValidateKeyPassword(string password)
        {
            if (!TryGetStorePassword(out var storePassword, out var error, true))
                return error;

            return KeystoreAliasReader.VerifyKeyPassword(KeystorePath, storePassword, KeyAlias, password, out error) ? null : error;
        }

        #endregion

        #region Aliases

        private IEnumerable<string> GetAliases() => _aliasCache ??= ReadAliases();

        private void RefreshAliases() => _aliasCache = ReadAliases();

        private List<string> ReadAliases()
        {
            _aliasError = null;
            if (!HasKeystoreFile)
                return new List<string>();

            if (!TryGetStorePassword(out var storePassword, out _aliasError, true))
                return new List<string>();

            return KeystoreAliasReader.TryReadAliases(KeystorePath, storePassword, out var aliases, out _aliasError)
                ? aliases
                : new List<string>();
        }

        #endregion

        private static string FromEnvironment(string variable, string fallback)
        {
            var value = Environment.GetEnvironmentVariable(variable);
            return string.IsNullOrEmpty(value) ? fallback : value;
        }

        private void InvalidateKeystore()
        {
            _aliasCache = null;
            _aliasError = null;
        }

        private void InvalidateAlias() => _aliasError = null;
    }
}
