// Author: František Holubec
// Created: 20.07.2026

using System;
using UnityEngine;

namespace EDIVE.BuildTool.Signing
{
    [Serializable]
    public class KeystoreCredentials
    {
        public string StoreFilePath;
        public string KeyAlias;
        public string StorePassword;
        public string KeyPassword;

        public bool IsComplete => !string.IsNullOrEmpty(StoreFilePath) &&
                                  !string.IsNullOrEmpty(KeyAlias) &&
                                  !string.IsNullOrEmpty(StorePassword) &&
                                  !string.IsNullOrEmpty(KeyPassword);

        public KeystoreCredentials Clone() => new()
        {
            StoreFilePath = StoreFilePath,
            KeyAlias = KeyAlias,
            StorePassword = StorePassword,
            KeyPassword = KeyPassword
        };

        public string ToJson() => JsonUtility.ToJson(this);

        public static KeystoreCredentials FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return null;
            try
            {
                return JsonUtility.FromJson<KeystoreCredentials>(json);
            }
            catch
            {
                return null;
            }
        }
    }
}
