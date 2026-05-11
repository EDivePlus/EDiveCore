// Author: František Holubec
// Created: 18.05.2025

using Cysharp.Threading.Tasks;
using EDIVE.Input.Keyboard;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.XRTools.Interactions;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

#if SR_DEBUGGER
using SRDebugger.Internal;
#endif

namespace EDIVE.Utils.SRDebugger
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
        private VirtualKeyboardProvider _KeyboardProvider;

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
            Service.Trigger.IsEnabled = false;
            _panelRect = SRDebug.Instance.EnableWorldSpaceMode();
            transform.AddChangeListener(OnTransformChanged);
            RepositionPanel();

            SRDebug.Instance?.HideDebugPanel();
            await UniTask.WaitForEndOfFrame();
            
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
                var keyboardProvider = _panelRect.gameObject.GetOrAddComponent<VirtualKeyboardProvider>();
                keyboardProvider.Keyboard = _KeyboardProvider.Keyboard;
            }
            
            var panelCanvas = _panelRect.GetComponentInChildren<Canvas>(true);
            if (_OverrideCanvasSorting)
            {
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
            
            if (panelCanvas)
            {
                panelCanvas.GetOrAddComponent<RectMask2D>();
            }
            
            // Fix panels not hiding properly when enabled at start
            SRDebug.Instance?.ShowDebugPanel();
            SRDebug.Instance?.HideDebugPanel();
       
            await UniTask.WaitForEndOfFrame();
            SRDebug.Instance?.ShowDebugPanel();
            SRDebug.Instance?.HideDebugPanel();
        }

        private void OnEnable()
        {
            SRDebug.Instance.PanelVisibilityChanged += OnPanelVisibilityChanged;
        }

        private void OnDisable()
        {
            if (SRDebug.Instance == null)
                return;
            SRDebug.Instance.HideDebugPanel(); 
            SRDebug.Instance.PanelVisibilityChanged -= OnPanelVisibilityChanged;
        }

        private void OnTransformChanged(Transform target) => RepositionPanel();

        private void RepositionPanel()
        {
            if (_panelRect == null || !SRDebug.Instance.IsDebugPanelVisible)
                return;

            var parent = _panelRect.parent;
            var parentLossy = parent != null ? parent.lossyScale : Vector3.one;
            var targetLossy = _ParentRect.lossyScale;

            _panelRect.pivot = _ParentRect.pivot;
            _panelRect.anchorMin = _panelRect.anchorMax = _ParentRect.pivot;
            _panelRect.sizeDelta = _ParentRect.rect.size;
            _panelRect.rotation = _ParentRect.rotation;
            _panelRect.localScale = new Vector3(
                parentLossy.x != 0 ? targetLossy.x / parentLossy.x : 1f,
                parentLossy.y != 0 ? targetLossy.y / parentLossy.y : 1f,
                parentLossy.z != 0 ? targetLossy.z / parentLossy.z : 1f);
            _panelRect.position = _ParentRect.position;
        }
        
        private void OnPanelVisibilityChanged(bool visible)
        {
            if (visible)
                RepositionPanel();
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

