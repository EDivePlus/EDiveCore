// Author: František Holubec
// Created: 20.07.2026

using System;
using UnityEditor;
using UnityEngine;

namespace EDIVE.BuildTool.Signing
{
    public static class KeystoreSecretStore
    {
        public const string ENV_PATH = "KEYSTORE_PATH";
        public const string ENV_ALIAS = "KEYSTORE_ALIAS";
        public const string ENV_STOREPASS = "KEYSTORE_STOREPASS";
        public const string ENV_KEYPASS = "KEYSTORE_KEYPASS";

        private const string PREFS_PREFIX = "EDIVE.BuildTool.AndroidKeystore:";

        public static bool TryLoad(string keystoreId, out KeystoreCredentials credentials)
        {
            credentials = null;
            var key = PrefsKey(keystoreId);
            if (string.IsNullOrEmpty(keystoreId) || !EditorPrefs.HasKey(key))
                return false;

            try
            {
                credentials = KeystoreCredentials.FromJson(KeystoreCrypto.Decrypt(EditorPrefs.GetString(key)));
            }
            catch
            {
                Debug.LogWarning($"[Keystore] Stored credentials for '{keystoreId}' could not be decrypted on this machine. Re-enter them.");
                return false;
            }
            return credentials != null;
        }

        public static void Save(string keystoreId, KeystoreCredentials credentials) =>
            EditorPrefs.SetString(PrefsKey(keystoreId), KeystoreCrypto.Encrypt(credentials.ToJson()));

        public static void Delete(string keystoreId) => EditorPrefs.DeleteKey(PrefsKey(keystoreId));

        public static bool TryResolve(string keystoreId, out KeystoreCredentials credentials)
        {
            var result = TryLoad(keystoreId, out var stored) && stored != null ? stored : new KeystoreCredentials();
            var found = stored != null;
            found |= OverlayEnvironment(result);

            credentials = found ? result : null;
            return found;
        }

        private static bool OverlayEnvironment(KeystoreCredentials target)
        {
            var any = false;
            any |= Apply(ENV_PATH, v => target.StoreFilePath = v);
            any |= Apply(ENV_ALIAS, v => target.KeyAlias = v);
            any |= Apply(ENV_STOREPASS, v => target.StorePassword = v);
            any |= Apply(ENV_KEYPASS, v => target.KeyPassword = v);
            return any;
        }

        private static bool Apply(string envName, Action<string> apply)
        {
            var value = Environment.GetEnvironmentVariable(envName);
            if (string.IsNullOrEmpty(value))
                return false;
            apply(value);
            return true;
        }

        private static string PrefsKey(string keystoreId) => PREFS_PREFIX + keystoreId;
    }
}
