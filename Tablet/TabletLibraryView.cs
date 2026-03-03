// Author: Michal Petr
// Created: 03.03.2026

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EDIVE.NativeUtils;
using UnityEngine;

namespace EDIVE.Tablet
{
    public class TabletLibraryView : ATabletView 
    {
        [SerializeField]
        private TabletDefinition _TabletDefinition;
        
        [SerializeField]
        private Transform _WidgetListRoot;
        
        [SerializeField]
        private TabletWidgetDefinitionDisplay _WidgetDisplayPrefab;
        
        private List<TabletWidgetDefinitionDisplay> WidgetDisplays { get; set; } = new();

        protected override async UniTask InitializeInternal()
        {
            _WidgetListRoot.DestroyChildren();
            
            foreach (var widgetDefinition in _TabletDefinition.Widgets)
            {
                var display = Instantiate(_WidgetDisplayPrefab, _WidgetListRoot);
                display.OnClick += OnWidgetDisplayClicked;
                display.SetDefinition(widgetDefinition);
                WidgetDisplays.Add(display);
            }
        }
        
        private void OnWidgetDisplayClicked(TabletWidgetDefinition definition)
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
