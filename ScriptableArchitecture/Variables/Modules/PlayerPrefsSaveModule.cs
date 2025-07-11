// Author: František Holubec
// Created: 06.05.2025

using System;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.ScriptableArchitecture.Variables.Modules
{
    [Serializable]
    public class PlayerPrefsSaveModule<T> : IScriptableVariableSaveModule<T>
    {
        [SerializeField]
        private string _SaveID;

        [SerializeField]
        private T _InitialValue;

        public string SaveID => $"{Application.productName}_{_SaveID}";

        public UniTask<(bool, T)> LoadValue()
        {
            if (!PlayerPrefs.HasKey(SaveID))
            {
                SaveValue(_InitialValue);
                return UniTask.FromResult((true, _InitialValue));
            }

            var stringValue = PlayerPrefs.GetString(SaveID);
            try
            {
                var value = JsonConvert.DeserializeObject<T>(stringValue);
                return UniTask.FromResult((true, value));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return UniTask.FromResult((false, default(T)));
            }
        }

        public UniTask SaveValue(T value)
        {
            PlayerPrefs.SetString(SaveID, JsonConvert.SerializeObject(value));
            return UniTask.CompletedTask;
        }
    }
}
