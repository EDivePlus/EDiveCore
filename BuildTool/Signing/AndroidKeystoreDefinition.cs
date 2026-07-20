// Author: František Holubec
// Created: 20.07.2026

using System;
using System.Collections.Generic;
using System.IO;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace EDIVE.BuildTool.Signing
{
    public class AndroidKeystoreDefinition : ScriptableObject
    {
        [NonSerialized]
        [ShowInInspector]
        [DelayedProperty]
        [InfoBox("Values are stored encrypted in EditorPrefs, not in the asset")]
        [Sirenix.OdinInspector.FilePath(Extensions = "keystore,jks,ks", AbsolutePath = true)]
        [OnValueChanged(nameof(RefreshChecks))]
        [EnhancedValidate(nameof(ValidatePath))]
        [LabelText("Keystore Path")]
        private string _keystorePath;

        [NonSerialized]
        [ShowInInspector]
        [PasswordField(Delayed = true)]
        [OnValueChanged(nameof(RefreshChecks))]
        [EnhancedValidate(nameof(ValidateStorePassword))]
        [LabelText("Store Password")]
        private string _localStorePassword;

        [NonSerialized]
        [ShowInInspector]
        [DelayedProperty]
        [EnhancedValueDropdown(nameof(GetAliases), true)]
        [OnValueChanged(nameof(RefreshChecks))]
        [EnhancedValidate(nameof(ValidateAlias))]
        [LabelText("Key Alias")]
        private string _localAlias;

        [NonSerialized]
        [ShowInInspector]
        [PasswordField(Delayed = true)]
        [OnValueChanged(nameof(RefreshChecks))]
        [EnhancedValidate(nameof(ValidateKeyPassword))]
        [LabelText("Key Password")]
        private string _localKeyPassword;

        [NonSerialized] private List<string> _aliases = new();
        [NonSerialized] private string _storeError;
        [NonSerialized] private string _aliasError;
        [NonSerialized] private string _keyError;

        [NonSerialized] private KeystoreCredentials _saved;
        [NonSerialized] private bool _savedLoaded;
        [NonSerialized] private bool _populated;

        [NonSerialized] private string _cachedId;
        public string Id => _cachedId ??= ResolveId();

        [ShowInInspector]
        [DisplayAsString]
        [LabelText("Status")]
        private string Status
        {
            get
            {
                var saved = Saved;
                if (saved == null || !saved.IsComplete)
                    return "Not configured";

                var upToDate = _keystorePath == saved.StoreFilePath
                    && _localAlias == saved.KeyAlias
                    && _localStorePassword == saved.StorePassword
                    && _localKeyPassword == saved.KeyPassword;
                return upToDate ? "Saved" : "Unsaved changes";
            }
        }

        private KeystoreCredentials Saved
        {
            get
            {
                if (_savedLoaded)
                    return _saved;
                _savedLoaded = true;
                KeystoreSecretStore.TryLoad(Id, out _saved);
                return _saved;
            }
        }

        private string ResolveId()
        {
            var path = AssetDatabase.GetAssetPath(this);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.AssetPathToGUID(path);
        }

        public bool TryResolve(out KeystoreCredentials credentials) => KeystoreSecretStore.TryResolve(Id, out credentials) && credentials.IsComplete;

        [OnInspectorInit]
        private void PopulateFromStore()
        {
            if (_populated)
                return;
            _populated = true;

            var saved = Saved;
            if (saved == null)
                return;

            _keystorePath = saved.StoreFilePath;
            _localAlias = saved.KeyAlias;
            _localStorePassword = saved.StorePassword;
            _localKeyPassword = saved.KeyPassword;
            RefreshChecks();
        }

        private IEnumerable<string> GetAliases() => _aliases;

        private void RefreshChecks()
        {
            _aliases = new List<string>();
            _storeError = _aliasError = _keyError = null;

            if (string.IsNullOrEmpty(_keystorePath) || !File.Exists(_keystorePath) || string.IsNullOrEmpty(_localStorePassword))
                return;

            if (!KeystoreAliasReader.TryReadAliases(_keystorePath, _localStorePassword, out _aliases, out _storeError))
                return;

            if (!string.IsNullOrEmpty(_localAlias) && !_aliases.Contains(_localAlias))
            {
                _aliasError = $"Alias '{_localAlias}' not found in keystore";
                return;
            }

            if (!string.IsNullOrEmpty(_localAlias) && !string.IsNullOrEmpty(_localKeyPassword))
                KeystoreAliasReader.VerifyKeyPassword(_keystorePath, _localStorePassword, _localAlias, _localKeyPassword, out _keyError);
        }

        private void ValidatePath(SelfValidationResult result)
        {
            if (string.IsNullOrEmpty(_keystorePath) || !File.Exists(_keystorePath))
                result.AddError("Keystore file not found");
        }

        private void ValidateStorePassword(SelfValidationResult result)
        {
            if (string.IsNullOrEmpty(_localStorePassword))
                result.AddError("Store password is required");
            else if (!string.IsNullOrEmpty(_storeError))
                result.AddError(_storeError);
        }

        private void ValidateAlias(SelfValidationResult result)
        {
            if (!string.IsNullOrEmpty(_aliasError))
                result.AddError(_aliasError);
        }

        private void ValidateKeyPassword(SelfValidationResult result)
        {
            if (string.IsNullOrEmpty(_localKeyPassword))
                result.AddError("Key password is required");
            else if (!string.IsNullOrEmpty(_keyError))
                result.AddError(_keyError);
        }

        [HorizontalGroup("Buttons")]
        [Button("Load")]
        private void LoadFromStore()
        {
            _savedLoaded = false;
            var saved = Saved;
            _keystorePath = saved?.StoreFilePath ?? string.Empty;
            _localAlias = saved?.KeyAlias ?? string.Empty;
            _localStorePassword = saved?.StorePassword ?? string.Empty;
            _localKeyPassword = saved?.KeyPassword ?? string.Empty;
            RefreshChecks();
        }

        [HorizontalGroup("Buttons")]
        [Button("Save")]
        private void SaveToStore()
        {
            if (string.IsNullOrEmpty(Id))
            {
                EditorUtility.DisplayDialog("Android Keystore", "Save the asset first so it has a stable GUID.", "OK");
                return;
            }

            KeystoreSecretStore.Save(Id, new KeystoreCredentials
            {
                StoreFilePath = _keystorePath,
                KeyAlias = _localAlias,
                StorePassword = _localStorePassword,
                KeyPassword = _localKeyPassword
            });
            _savedLoaded = false;
        }

        [HorizontalGroup("Buttons")]
        [Button("Clear")]
        private void ClearStore()
        {
            if (!EditorUtility.DisplayDialog("Android Keystore", "Clear the stored credentials?", "Clear", "Cancel"))
                return;

            KeystoreSecretStore.Delete(Id);
            _keystorePath = _localAlias = _localStorePassword = _localKeyPassword = string.Empty;
            _savedLoaded = false;
            RefreshChecks();
        }

        [PropertySpace]
        [PropertyOrder(100)]
        [ShowAsString(Overflow = false)]
        [ShowInInspector]
        private string EnvVariables => $"{KeystoreSecretStore.ENV_PATH}\n" +
                                       $"{KeystoreSecretStore.ENV_ALIAS}\n" +
                                       $"{KeystoreSecretStore.ENV_STOREPASS}\n" +
                                       $"{KeystoreSecretStore.ENV_KEYPASS}";
    }
}
