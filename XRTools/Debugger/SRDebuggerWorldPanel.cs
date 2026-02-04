// Author: František Holubec
// Created: 18.05.2025

using Cysharp.Threading.Tasks;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

#if SR_DEBUGGER
using EDIVE.NativeUtils;
using EDIVE.XRTools.Interactions;
using EDIVE.XRTools.Keyboard;
using UnityEngine.UI;
#endif

namespace EDIVE.Utils
{
    public class SRDebuggerWorldPanel : MonoBehaviour
    {
#if SR_DEBUGGER
        [SerializeField]
        private RectTransform _ParentRect;

        [SerializeField]
        private FilteredTrackedDeviceGraphicRaycaster _XRRaycaster;

        [SerializeField]
        private bool _OverrideCanvasSorting = true;
        
        [SerializeField]
        private KeyboardProvider _KeyboardProvider;

        [ShowIf(nameof(_OverrideCanvasSorting))]
        [SortingLayer]
        [SerializeField]
        private int _CanvasSortingLayer;

        [ShowIf(nameof(_OverrideCanvasSorting))]
        [SerializeField]
        private int _CanvasSortingOrder = 1000;

        private RectTransform _panelRect;

        private async UniTaskVoid Start()
        {
            _panelRect = SRDebug.Instance.EnableWorldSpaceMode();
            if (_XRRaycaster)
            {
                foreach (var raycaster in _panelRect.GetComponentsInChildren<GraphicRaycaster>(true))
                {
                    var xrRaycaster = raycaster.GetOrAddComponent<FilteredTrackedDeviceGraphicRaycaster>();
                    CopyTrackedDeviceGraphicRaycasterData(xrRaycaster, _XRRaycaster);
                }
            }

            if (_KeyboardProvider != null)
            {
                var keyboardProvider = _panelRect.gameObject.GetOrAddComponent<KeyboardProvider>();
                keyboardProvider.Keyboard = _KeyboardProvider.Keyboard;
            }
            
            transform.AddChangeListener(OnTransformChanged);
            RepositionPanel();

            await UniTask.WaitForEndOfFrame();
            if (_OverrideCanvasSorting)
            {
                var panelCanvas = _panelRect.GetComponentInChildren<Canvas>(true);
                if (panelCanvas)
                {
                    panelCanvas.sortingLayerID = _CanvasSortingLayer;
                    panelCanvas.sortingOrder = _CanvasSortingOrder;
                    
                    var panelCanvasChildren = _panelRect.GetComponentsInChildren<Canvas>(true);
                    foreach (var childCanvas in panelCanvasChildren)
                    {
                        if (childCanvas == panelCanvas)
                            continue;
                        childCanvas.sortingLayerID = _CanvasSortingLayer;
                        childCanvas.sortingOrder = _CanvasSortingOrder + 1;
                    }
                }
            }
            SRDebug.Instance?.HideDebugPanel();
        }

        private void OnDisable()
        {
            SRDebug.Instance?.HideDebugPanel();
        }

        private void OnTransformChanged(Transform target) => RepositionPanel();

        private void RepositionPanel()
        {
            if (_panelRect == null)
                return;

            var prevParent = _panelRect.parent;
            _panelRect.SetParent(_ParentRect);
            _panelRect.SetToFillParent();
            _panelRect.SetParent(prevParent);
        }

        private static void CopyTrackedDeviceGraphicRaycasterData(FilteredTrackedDeviceGraphicRaycaster target, FilteredTrackedDeviceGraphicRaycaster original)
        {
            target.ignoreReversedGraphics = original.ignoreReversedGraphics;
            target.blockingMask = original.blockingMask;
            target.checkFor2DOcclusion = original.checkFor2DOcclusion;
            target.checkFor3DOcclusion = original.checkFor3DOcclusion;
            target.raycastTriggerInteraction = original.raycastTriggerInteraction;
            target.InteractionLayers = original.InteractionLayers;
        }
#endif
    }
}

