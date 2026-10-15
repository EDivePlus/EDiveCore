// Author: František Holubec
// Created: 15.10.2026

using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.External.DomainReloadHelper;
using UnityEngine;

namespace EDIVE.Debugging
{
    // Favourite controls by key, saved per device. A missing control stays saved until it registers again.
    public static class DebugFavourites
    {
        private const string PREFS_KEY = "EDIVE.DebugMenu.Favourites";

        [ClearOnReload(NewInstance = true)]
        private static readonly List<string> KEYS = new();

        [ClearOnReload]
        private static bool _loaded;

        [ClearOnReload]
        public static event Action Changed;

        public static IReadOnlyList<string> Keys
        {
            get
            {
                Load();
                return KEYS;
            }
        }

        public static bool Contains(ADebugControl control)
        {
            var key = control?.Key;
            return key != null && Keys.Contains(key);
        }

        public static void Toggle(ADebugControl control)
        {
            var key = control?.Key;
            if (key == null)
                return;

            Load();
            if (!KEYS.Remove(key))
                KEYS.Add(key);
            Save();
            Changed?.Invoke();
        }

        // Registered favourite controls, in the order they were added.
        public static List<ADebugControl> FindControls()
        {
            var byKey = new Dictionary<string, ADebugControl>();
            foreach (var control in DebugMenu.AllControls())
            {
                var key = control.Key;
                if (key != null)
                    byKey.TryAdd(key, control);
            }

            return Keys.Where(byKey.ContainsKey).Select(k => byKey[k]).ToList();
        }

        private static void Load()
        {
            if (_loaded)
                return;
            _loaded = true;
            KEYS.Clear();
            var saved = PlayerPrefs.GetString(PREFS_KEY, string.Empty);
            if (saved.Length > 0)
                KEYS.AddRange(saved.Split('\n').Where(k => k.Length > 0));
        }

        private static void Save()
        {
            PlayerPrefs.SetString(PREFS_KEY, string.Join("\n", KEYS));
            PlayerPrefs.Save();
        }
    }
}
