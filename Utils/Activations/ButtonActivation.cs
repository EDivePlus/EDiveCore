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
    public class ButtonActivation : AWrapperActivation
    {
        [SerializeField]
        private Button _Button;
        
        protected override void StartListening()
        {
            if (_Button != null)
                _Button.onClick.AddListener(OnButtonClick);
        }

        protected override void StopListening()
        {
            if (_Button != null)
                _Button.onClick.RemoveListener(OnButtonClick);
        }

        private void OnButtonClick() => InvokeListeners();
        
#if UNITY_EDITOR
        [OnInspectorInit]
        private void OnInspectorInit(InspectorProperty property)
        {
            if (_Button == null && property.SerializationRoot.ValueEntry.WeakSmartValue is MonoBehaviour monoBehaviour && monoBehaviour.TryGetComponent<Button>(out var button))
            {
                _Button = button;
                property.MarkSerializationRootDirty();
            }
        }
#endif
    }
}
