// Author: František Holubec
// Created: 07.04.2026

using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
#endif

namespace EDIVE.Forms.Controllers
{
    public interface IOptionSelector
    {
        void Initialize(Action<IOptionSelector, bool> callback);
        void Terminate();
        void SetSelected(bool selected, bool notify = true);
    }
    
    [Serializable]
    public class CompoundOptionSelector : IOptionSelector
    {
        [SerializeReference]
        private List<IOptionSelector> _Selectors = new();
        
        private Action<IOptionSelector, bool> _callback;

        public void Initialize(Action<IOptionSelector, bool> callback)
        {
            _callback = callback;
            foreach (var selector in _Selectors)
                selector?.Initialize(ChildSelectorCallback);
        }

        public void Terminate()
        {
            foreach (var selector in _Selectors)
                selector?.Terminate();
        }

        private void ChildSelectorCallback(IOptionSelector selector, bool value)
        {
            // Toggle silently all other selectors
            foreach (var childSelector in _Selectors)
            {
                if (childSelector == selector)
                    continue;
                childSelector?.SetSelected(value, false);
            }
            _callback?.Invoke(this, value);
        }
        
        public void SetSelected(bool selected, bool notify = true)
        {
            foreach (var selector in _Selectors)
                selector?.SetSelected(selected, notify);
        }
    }
    
    [Serializable]
    public class ToggleOptionSelector : IOptionSelector
    {
        [SerializeField]
        private Toggle _Toggle;
        
        private Action<IOptionSelector, bool> _callback;
        
        public void Initialize(Action<IOptionSelector, bool> callback)
        {
            _callback = callback;
            if (_Toggle != null)
                _Toggle.onValueChanged.AddListener(OnToggleValueChanged);
        }

        public void Terminate()
        {
            _callback = null;
            if (_Toggle != null)
                _Toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
        }

        private void OnToggleValueChanged(bool value) => _callback?.Invoke(this, value);
        
        public void SetSelected(bool selected, bool notify = true)
        {
            if (_Toggle)
            {
                if (notify)
                    _Toggle.isOn = selected;
                else
                    _Toggle.SetIsOnWithoutNotify(selected);
            }
        }
        
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
