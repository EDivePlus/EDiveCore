// Author: František Holubec
// Created: 18.05.2025

#if SR_DEBUGGER && XR_INTERACTION_TOOLKIT
using Cysharp.Threading.Tasks;
using EDIVE.NativeUtils;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace EDIVE.Utils
{
    public class SRDebuggerWorldPanel : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _ParentRect;

        [SerializeField]
        private TrackedDeviceGraphicRaycaster _XRRaycaster;

        private RectTransform _panelRect;

        private void Start()
        {
            _panelRect = SRDebug.Instance.EnableWorldSpaceMode();
            _panelRect.SetParent(_ParentRect, false);
            _panelRect.SetToFillParent();
            if (_XRRaycaster)
            {
                foreach (var raycaster in _panelRect.GetComponentsInChildren<GraphicRaycaster>(true))
                {
                    var xrRaycaster = raycaster.GetOrAddComponent<TrackedDeviceGraphicRaycaster>();
                    CopyTrackedDeviceGraphicRaycasterData(xrRaycaster, _XRRaycaster);
                }
            }
            SRDebug.Instance?.HideDebugPanel();
        }

        private static void CopyTrackedDeviceGraphicRaycasterData(TrackedDeviceGraphicRaycaster target, TrackedDeviceGraphicRaycaster original)
        {
            target.ignoreReversedGraphics = original.ignoreReversedGraphics;
            target.blockingMask = original.blockingMask;
            target.checkFor2DOcclusion = original.checkFor2DOcclusion;
            target.checkFor3DOcclusion = original.checkFor3DOcclusion;
            target.raycastTriggerInteraction = original.raycastTriggerInteraction;
        }
    }
}
#endif
