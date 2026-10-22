// Author: František Holubec
// Created: 20.10.2026

using System.Collections;
using UnityEngine;
using UnityEngine.XR;

namespace EDIVE.ServiceHub.Streaming.Sources
{
    // What this user sees, rendered into a texture for publishing
    public class ViewStreamSource : MonoBehaviour
    {
        public enum FlipMode
        {
            Auto,
            Flip,
            Keep
        }

        [SerializeField]
        private Vector2Int _Resolution = new(1280, 720);

        [SerializeField]
        [Tooltip("Vertical flip when sending the screen capture. Auto depends on the graphics API")]
        private FlipMode _ScreenCaptureFlip;

        [SerializeField]
        [Tooltip("Vertical flip when sending the headset copy camera. Auto depends on the graphics API")]
        private FlipMode _CopyCameraFlip;

        private RenderTexture _output;
        private RenderTexture _screen;
        private Camera _copyCamera;
        private Coroutine _capture;
        private bool _includeMenus = true;

        public RenderTexture Output => _output;

        // Screen capture on top-down UV APIs (D3D, Metal, Vulkan) already comes upside down
        public bool FlipVertically => (UsesCopyCamera ? _CopyCameraFlip : _ScreenCaptureFlip) switch
        {
            FlipMode.Flip => true,
            FlipMode.Keep => false,
            _ => UsesCopyCamera || !SystemInfo.graphicsUVStartsAtTop
        };
        public bool IsRunning => _output != null;

        // Headset and hidden menus render a copy of the main camera, flat screens capture the screen with menus
        public bool UsesCopyCamera => XRSettings.isDeviceActive || !_includeMenus;

        public bool IncludeMenus
        {
            get => _includeMenus;
            set
            {
                if (_includeMenus == value)
                    return;
                _includeMenus = value;
                if (_output != null)
                    StartCapture();
            }
        }

        public RenderTexture Begin()
        {
            if (_output != null)
                return _output;

            _output = new RenderTexture(_Resolution.x, _Resolution.y, 24) { name = "ViewStream" };
            _output.Create();
            StartCapture();
            return _output;
        }

        public void End()
        {
            StopCapture();
            Release(ref _output);
        }

        private void StartCapture()
        {
            StopCapture();
            if (UsesCopyCamera)
                CreateCopyCamera();
            else
                _capture = StartCoroutine(CaptureScreen());
        }

        private void StopCapture()
        {
            if (_capture != null)
                StopCoroutine(_capture);
            _capture = null;

            if (_copyCamera)
                Destroy(_copyCamera.gameObject);
            _copyCamera = null;

            Release(ref _screen);
        }

        private void OnDestroy() => End();

        private void LateUpdate()
        {
            // Main camera can change, e.g. after a scene load
            if (_output != null && UsesCopyCamera && (_copyCamera == null || _copyCamera.transform.parent != MainCameraTransform))
                CreateCopyCamera();
        }

        private static Transform MainCameraTransform => Camera.main ? Camera.main.transform : null;

        private void CreateCopyCamera()
        {
            if (_copyCamera)
                Destroy(_copyCamera.gameObject);
            _copyCamera = null;

            var main = Camera.main;
            if (main == null)
                return;

            var go = new GameObject("ViewStreamCamera");
            go.transform.SetParent(main.transform, false);
            _copyCamera = go.AddComponent<Camera>();
            _copyCamera.CopyFrom(main);
            _copyCamera.stereoTargetEye = StereoTargetEyeMask.None;
            _copyCamera.targetTexture = _output;
            _copyCamera.depth = main.depth - 1;
            if (!_includeMenus)
                _copyCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("UI"));
        }

        private IEnumerator CaptureScreen()
        {
            var endOfFrame = new WaitForEndOfFrame();
            while (true)
            {
                yield return endOfFrame;
                if (_screen == null || _screen.width != Screen.width || _screen.height != Screen.height)
                {
                    Release(ref _screen);
                    _screen = new RenderTexture(Screen.width, Screen.height, 0) { name = "ViewStreamScreen" };
                    _screen.Create();
                }
                ScreenCapture.CaptureScreenshotIntoRenderTexture(_screen);
                Graphics.Blit(_screen, _output);
            }
        }

        private static void Release(ref RenderTexture texture)
        {
            if (texture == null)
                return;
            texture.Release();
            Destroy(texture);
            texture = null;
        }
    }
}
