// Author: František Holubec
// Created: 04.09.2025

using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
#endif

namespace EDIVE.Utils.Activations
{
    [Serializable]
    public class ButtonActivation : IActivation
    {
        [SerializeField]
        private Button _Button;
        
        public void RegisterActivationListener(Action onActivate)
        {
            if (_Button != null)
                _Button.onClick.AddListener(onActivate.Invoke);
        }
        
        public void UnregisterActivationListener(Action onActivate)
        {
            if (_Button != null)
                _Button.onClick.RemoveListener(onActivate.Invoke);
        }

#if UNITY_EDITOR
        [OnInspectorInit]
        private void OnInspectorInit(InspectorProperty property)
        {
            if (property.SerializationRoot.ValueEntry.WeakSmartValue is MonoBehaviour monoBehaviour && monoBehaviour.TryGetComponent<Button>(out var button))
            {
                _Button = button;
                property.MarkSerializationRootDirty();
            }
        }
#endif
    }
}
