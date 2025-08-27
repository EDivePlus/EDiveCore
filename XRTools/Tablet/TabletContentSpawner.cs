// Author: František Holubec
// Created: 27.08.2025

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading.Loadables;
using EDIVE.Core;
using EDIVE.NativeUtils;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.XRTools.Tablet
{
    [Serializable]
    public class TabletContentSpawner : ILoadable
    {
        private enum TabletLayer
        {
            Main,
            Overlay,
            Debug
        }

        [SerializeField]
        private RectTransform _Panel;
        
        [SerializeField]
        private TabletLayer _Layer = TabletLayer.Main;

        [SerializeField]
        private bool _SetOrder;
        
        [ShowIf(nameof(_SetOrder))]
        [SerializeField]
        private int _Order;
        
        public async UniTask Load(Action<float> progressCallback)
        {
            var tablet = await AppCore.Services.AwaitRegistered<TabletController>();

            var layerCanvas = _Layer switch
            {
                TabletLayer.Main => tablet.MainLayer,
                TabletLayer.Overlay => tablet.OverlayLayer,
                TabletLayer.Debug => tablet.DebugLayer,
                _ => throw new ArgumentOutOfRangeException()
            };
            
            var panelInstance = Object.Instantiate(_Panel, layerCanvas.transform);
            panelInstance.SetToFillParent();
            if (_SetOrder)
                panelInstance.SetSiblingIndex(_Order);
        }
    }
}
