// Author: František Holubec
// Created: 15.09.2026

using System;
using UnityEditor;
using UnityEngine;

namespace EDIVE.BuildTool.Signing
{
    public readonly struct AndroidSigningData
    {
        public string KeystorePath { get; }
        public string KeyAlias { get; }
        public string StorePassword { get; }
        public string KeyPassword { get; }

        public AndroidSigningData(string keystorePath, string keyAlias, string storePassword, string keyPassword)
        {
            KeystorePath = keystorePath;
            KeyAlias = keyAlias;
            StorePassword = storePassword;
            KeyPassword = keyPassword;
        }

        public void Apply()
        {
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = KeystorePath;
            PlayerSettings.Android.keyaliasName = KeyAlias;
            PlayerSettings.Android.keystorePass = StorePassword;
            PlayerSettings.Android.keyaliasPass = KeyPassword;
        }

        public bool TryVerify(out string error)
        {
            error = null;
            // A missing JDK must not fail a build whose credentials are actually fine.
            if (!KeystoreAliasReader.IsAvailable)
            {
                Debug.LogWarning("[Signing] 'keytool' was not found, skipping keystore verification.");
                return true;
            }

            if (!KeystoreAliasReader.TryReadAliases(KeystorePath, StorePassword, out var aliases, out error))
                return false;

            // Keystore aliases are case insensitive and keytool reports them lowercased.
            var keyAlias = KeyAlias;
            if (!aliases.Exists(alias => string.Equals(alias, keyAlias, StringComparison.OrdinalIgnoreCase)))
            {
                error = $"Alias '{KeyAlias}' not found, keystore contains {string.Join(", ", aliases)}.";
                return false;
            }

            return KeystoreAliasReader.VerifyKeyPassword(KeystorePath, StorePassword, KeyAlias, KeyPassword, out error);
        }
    }
}
