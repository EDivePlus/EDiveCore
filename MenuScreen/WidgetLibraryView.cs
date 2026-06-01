// Author: Michal Petr
// Created: 03.03.2026

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EDIVE.NativeUtils;
using UnityEngine;

namespace EDIVE.MenuScreen
{
    public class WidgetLibraryView : WidgetView 
    {
        [SerializeField]
        private MenuScreenDefinition _MenuScreenDefinition;
        
        [SerializeField]
        private Transform _WidgetListRoot;
        
        [SerializeField]
        private WidgetDefinitionDisplay _WidgetDisplayPrefab;
        
        private List<WidgetDefinitionDisplay> WidgetDisplays { get; set; } = new();

        protected override async UniTask InitializeInternal()
        {
            _WidgetListRoot.DestroyChildren();
            
            foreach (var widgetDefinition in _MenuScreenDefinition.Widgets)
            {
                var display = Instantiate(_WidgetDisplayPrefab, _WidgetListRoot);
                display.OnClick += OnWidgetDisplayClicked;
                display.SetDefinition(widgetDefinition);
                WidgetDisplays.Add(display);
            }
        }
        
        private void OnWidgetDisplayClicked(WidgetDefinition definition)
        {
            Controller.OpenWidget(definition);
        }

        public override void OnTerminate()
        {
            foreach (var display in WidgetDisplays)
            {
                Destroy(display.gameObject);
            }
            WidgetDisplays.Clear();
        }
    }
}
