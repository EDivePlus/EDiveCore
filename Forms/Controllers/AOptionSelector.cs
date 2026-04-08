// Author: František Holubec
// Created: 07.04.2026

using System;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.Forms.Controllers
{
    public interface IOptionSelector
    {
        void RegisterSelectionListener(Action<bool> onSelected);
        void UnregisterSelectionListener(Action<bool> onSelected);
        void SetSelected(bool selected, bool notify = true);
        void SetInteractable(bool interactable);
    }
    
    [Serializable]
    public abstract class AWrapperOptionSelector : IOptionSelector
    {
        private event Action<bool> InnerEvent;

        public void RegisterSelectionListener(Action<bool> onSelected)
        {
            var wasEmpty = InnerEvent == null;
            InnerEvent += onSelected;
            if (wasEmpty)
                StartListening();
        }

        public void UnregisterSelectionListener(Action<bool> onSelected)
        {
            InnerEvent -= onSelected;
            if (InnerEvent == null)
                StopListening();
        }
        
        protected void InvokeListeners(bool value) => InnerEvent?.Invoke(value);
        
        protected abstract void StartListening();
        protected abstract void StopListening();
        
        public abstract void SetSelected(bool selected, bool notify = true);
        public abstract void SetInteractable(bool interactable);
    }
    
    [Serializable]
    public class ToggleOptionSelector : AWrapperOptionSelector
    {
        [SerializeField]
        private Toggle _Toggle;
        
        protected override void StartListening()
        {
            if (_Toggle != null)
                _Toggle.onValueChanged.AddListener(OnToggled);
        }

        protected override void StopListening()
        {
            if (_Toggle != null)
                _Toggle.onValueChanged.RemoveListener(OnToggled);
        }
        
        public override void SetSelected(bool selected, bool notify = true)
        {
            if (_Toggle)
            {
                if (notify)
                    _Toggle.isOn = selected;
                else
                    _Toggle.SetIsOnWithoutNotify(selected);
            }
        }
        
        public override void SetInteractable(bool interactable)
        {
            if (_Toggle)
            {
                _Toggle.interactable = interactable;
            }
        }

        private void OnToggled(bool value) => InvokeListeners(value);
        
#if UNITY_EDITOR
        [OnInspectorInit]
        private void OnInspectorInit(InspectorProperty property)
        {
            if (_Toggle == null && property.SerializationRoot.ValueEntry.WeakSmartValue is MonoBehaviour mb && mb.TryGetComponent<Toggle>(out var toggle))
            {
                _Toggle = toggle;
                property.MarkSerializationRootDirty();
            }
        }
#endif
    }
}
