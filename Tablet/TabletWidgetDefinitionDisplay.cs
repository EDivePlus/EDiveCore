// Author: Michal Petr
// Created: 03.03.2026

using System;
using EDIVE.UIElements.Tooltips;
using EDIVE.VisualPresets.Switchers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EDIVE.Tablet
{
    public class TabletWidgetDefinitionDisplay : MonoBehaviour
    {
        [SerializeField]
        private VisualSwitcher _Switcher;

        [SerializeField]
        private Button _OpenButton;
        
        [SerializeField]
        private TooltipTrigger _TooltipTrigger;
        
        public TabletWidgetDefinition Definition { get; private set; }
        public event Action<TabletWidgetDefinition> OnClick = delegate { };

        private void Awake()
        {
            _OpenButton.onClick.AddListener(OnOpenButtonClicked);
        }

        private void OnDestroy()
        {
            _OpenButton?.onClick.RemoveListener(OnOpenButtonClicked);
        }

        public void SetDefinition(TabletWidgetDefinition definition)
        {
            if (definition == null)
                return;
            
            Definition = definition;
            _Switcher.Apply(definition.Visual);
            if (_TooltipTrigger)
                _TooltipTrigger.SetPreset(definition.Visual);
        }

        private void OnOpenButtonClicked()
        {
            OnClick?.Invoke(Definition);
        }
    }
}
