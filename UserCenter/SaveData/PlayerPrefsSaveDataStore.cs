// Author: Radim Holub
// Created: 19.02.2026

using UnityEngine;

namespace EDIVE.UserCenter.SaveData
{
    public sealed class PlayerPrefsSaveDataStore : ISaveDataLocalStore
    {
        private readonly string _prefix;

        public PlayerPrefsSaveDataStore(string prefix = "uc.savedata.")
        {
            _prefix = prefix ?? "uc.savedata.";
        }

        private string BuildKey(string key) => _prefix + (key ?? "");

        public bool TryGet(string key, out string json)
        {
            var k = BuildKey(key);
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
            PlayerPrefs.SetString(BuildKey(key), json ?? "");
        }

        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(BuildKey(key));
        }

        public void Save()
        {
            PlayerPrefs.Save();
        }
    }
}

