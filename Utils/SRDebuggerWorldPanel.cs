// Author: František Holubec
// Created: 18.05.2025

using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

#if SR_DEBUGGER && XR_INTERACTION_TOOLKIT
using EDIVE.NativeUtils;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
#endif

namespace EDIVE.Utils
{
    public class SRDebuggerWorldPanel : MonoBehaviour
    {
#if SR_DEBUGGER && XR_INTERACTION_TOOLKIT
        [SerializeField]
        private RectTransform _ParentRect;

        [SerializeField]
        private TrackedDeviceGraphicRaycaster _XRRaycaster;

        [SerializeField]
        private bool _OverrideCanvasSorting = true;

        [ShowIf(nameof(_OverrideCanvasSorting))]
        [SortingLayer]
        [SerializeField]
        private int _CanvasSortingLayer;

        [ShowIf(nameof(_OverrideCanvasSorting))]
        [SerializeField]
        private int _CanvasSortingOrder = 1000;

        private RectTransform _panelRect;

        private void Start()
        {
            _panelRect = SRDebug.Instance.EnableWorldSpaceMode();
            if (_XRRaycaster)
            {
                foreach (var raycaster in _panelRect.GetComponentsInChildren<GraphicRaycaster>(true))
                {
                    var xrRaycaster = raycaster.GetOrAddComponent<TrackedDeviceGraphicRaycaster>();
                    CopyTrackedDeviceGraphicRaycasterData(xrRaycaster, _XRRaycaster);
                }
            }

            if (_OverrideCanvasSorting)
            {
                var panelCanvas = _panelRect.GetComponentInChildren<Canvas>();
                if (panelCanvas)
                {
                    panelCanvas.sortingLayerID = _CanvasSortingLayer;
                    panelCanvas.sortingOrder = _CanvasSortingOrder;
                }
            }

            transform.AddChangeListener(OnTransformChanged);
            RepositionPanel();

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

        private static void CopyTrackedDeviceGraphicRaycasterData(TrackedDeviceGraphicRaycaster target, TrackedDeviceGraphicRaycaster original)
        {
            target.ignoreReversedGraphics = original.ignoreReversedGraphics;
            target.blockingMask = original.blockingMask;
            target.checkFor2DOcclusion = original.checkFor2DOcclusion;
            target.checkFor3DOcclusion = original.checkFor3DOcclusion;
            target.raycastTriggerInteraction = original.raycastTriggerInteraction;
        }
#endif
    }
}

