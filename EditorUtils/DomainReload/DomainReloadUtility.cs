using System;
using System.Collections;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace EDIVE.EditorUtils.DomainReload
{
    public static class DomainReloadUtility
    {
        private const string SURVIVOR_SESSION_STATE_KEY = "DomainReloadSurvivors";

        [DidReloadScripts]
        public static void OnAfterDomainReload()
        {
            // Delay the invocation to the next frame so coroutines are not interrupted
            EditorApplication.delayCall += OnDelayedDomainReload;
        }

        public static void OnDelayedDomainReload()
        {
            EditorCoroutineUtility.StartCoroutineOwnerless(InvokeSurvivors());
        }

        private static IEnumerator InvokeSurvivors()
        {
            while (EditorApplication.isCompiling)
                yield return null;

            yield return null; // Wait one more frame so that there is time to clear unnecessary survivors

            var survivors = GetSurvivors();
            survivors.Sort();
            foreach (var survivor in survivors)
            {
                if (survivor.Survivor == null)
                {
                    DebugLite.LogError($"[DomainReloadUtility] Survivor with ID '{survivor.ID}' is null. Skipping...");
                    continue;
                }

                DebugLite.Log($"[DomainReloadUtility] Invoking survivor with ID '{survivor.ID}'");
                survivor.Survivor?.OnAfterDomainReload();
            }
            SetSurvivors(null);
        }

        public static void RegisterSurvivor(string survivorID, IDomainReloadSurvivor survivor, int priority = 0)
        {
            DebugLite.Log($"[DomainReloadUtility] Registering survivor with ID '{survivorID}'");
            var survivors = GetSurvivors();
            if (survivors.RemoveAll(s => s.ID == survivorID) > 0)
            {
                DebugLite.LogError($"[DomainReloadUtility] Survivor with ID '{survivorID}' already exists. Overwriting...");
            }
            survivors.Add(new SurvivorWrapper(survivorID, priority, survivor));
            SetSurvivors(survivors);
        }

        public static void ClearSurvivor(string survivorID)
        {
            DebugLite.Log($"[DomainReloadUtility] Clearing survivor with ID '{survivorID}'");
            var survivors = GetSurvivors();
            survivors.RemoveAll(s => s.ID == survivorID);
            SetSurvivors(survivors);
        }

        private static List<SurvivorWrapper> GetSurvivors()
        {
            try
            {
                var json = SessionState.GetString(SURVIVOR_SESSION_STATE_KEY, null);
                return JsonUtility.FromJson<SaveData>(json).Survivors;
            }
            catch (Exception)
            {
                return new List<SurvivorWrapper>();
            }
        }

        private static void SetSurvivors(List<SurvivorWrapper> survivors)
        {
            SessionState.SetString(SURVIVOR_SESSION_STATE_KEY, JsonUtility.ToJson(new SaveData(survivors)));
        }

        [Serializable]
        private class SaveData
        {
            public List<SurvivorWrapper> Survivors;
            public SaveData(List<SurvivorWrapper> survivors) { Survivors = survivors; }
        }

        [Serializable]
        private class SurvivorWrapper : IComparable<SurvivorWrapper>
        {
            [SerializeField]
            private string _ID;

            [SerializeField]
            private int _Priority;

            [SerializeReference]
            private IDomainReloadSurvivor _Survivor;

            public string ID => _ID;
            public int Priority => _Priority;
            public IDomainReloadSurvivor Survivor => _Survivor;

            public SurvivorWrapper(string id, int priority, IDomainReloadSurvivor survivor)
            {
                _ID = id;
                _Priority = priority;
                _Survivor = survivor;
            }

            public int CompareTo(SurvivorWrapper other)
            {
                return Priority.CompareTo(other.Priority);
            }
        }
    }
}
