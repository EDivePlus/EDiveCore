// Author: Radim Holub
// Created: 19.02.2026

using UnityEngine;

namespace EDIVE.UserCenter
{
    public interface ISavedataLocalStore
    {
        bool TryGet(string key, out string json);
        void Set(string key, string json);
        void Delete(string key);
    }

    public sealed class PlayerPrefsSavedataStore : ISavedataLocalStore
    {
        private readonly string _prefix;

        public PlayerPrefsSavedataStore(string prefix = "uc.savedata.")
        {
            _prefix = prefix ?? "uc.savedata.";
        }

        private string K(string key) => _prefix + (key ?? "");

        public bool TryGet(string key, out string json)
        {
            var k = K(key);
            if (!PlayerPrefs.HasKey(k))
            {
                json = null;
                return false;
            }

            json = PlayerPrefs.GetString(k, "");
            return !string.IsNullOrWhiteSpace(json);
        }

        public void Set(string key, string json)
        {
            PlayerPrefs.SetString(K(key), json ?? "");
            PlayerPrefs.Save();
        }

        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(K(key));
        }
    }
}

