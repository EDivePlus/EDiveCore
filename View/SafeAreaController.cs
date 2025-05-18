using System;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_IOS
using System.Runtime.InteropServices;
#endif

namespace EDIVE.View
{
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaController : MonoBehaviour
    {
        [Flags]
        private enum SafeAreaUpdateSide
        {
            Left = 1 << 0,
            Right = 1 << 1,
            Top = 1 << 2,
            Bottom = 1 << 3,
            All = Left | Right | Top | Bottom
        }

        [SerializeField]
        private SafeAreaUpdateSide _UpdateSides = SafeAreaUpdateSide.All;

        private Rect _currentSafeArea = new(0, 0, 0, 0);
        private Vector2Int _currentScreenSize = new(0, 0);
        private ScreenOrientation _currentOrientation;
        private Canvas _canvas;

        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>(true);
            Refresh();
        }

        private void Update()
        {
            if (CheckSafeAreaChange())
                Refresh();
        }

        private bool CheckSafeAreaChange()
        {
            var newSafeArea = GetSafeArea();
            return _currentSafeArea != newSafeArea || _currentScreenSize.x != Screen.width || _currentScreenSize.y != Screen.height || _currentOrientation != Screen.orientation;
        }

        [Button]
        private void Refresh()
        {
            _currentSafeArea = GetSafeArea();
            _currentScreenSize = new Vector2Int(Screen.width, Screen.height);
            _currentOrientation = Screen.orientation;

            // Convert safe area rectangle from absolute pixels to normalized anchor coordinates
            var anchorMin = _currentSafeArea.position;
            var anchorMax = _currentSafeArea.position + _currentSafeArea.size;

            float canvasWidth = Screen.width;
            float canvasHeight = Screen.height;
            
            if (_canvas)
            {
                var pixelRect = _canvas.pixelRect;
                canvasWidth = pixelRect.width;
                canvasHeight = pixelRect.height;
            }

            anchorMin.x /= canvasWidth;
            anchorMin.y /= canvasHeight;
            anchorMax.x /= canvasWidth;
            anchorMax.y /= canvasHeight;

            if (!ShouldUpdateSide(SafeAreaUpdateSide.Left)) anchorMin.x = 0;
            if (!ShouldUpdateSide(SafeAreaUpdateSide.Bottom)) anchorMin.y = 0;
            if (!ShouldUpdateSide(SafeAreaUpdateSide.Right)) anchorMax.x = 0;
            if (!ShouldUpdateSide(SafeAreaUpdateSide.Top)) anchorMax.y = 0;

            var targetRect = GetComponent<RectTransform>();
            targetRect.anchorMin = anchorMin;
            targetRect.anchorMax = anchorMax;
        }

        private bool ShouldUpdateSide(SafeAreaUpdateSide side)
        {
            return (_UpdateSides & side) != 0;
        }

        [Button]
        private void SetWholeScreen()
        {
            var targetRect = GetComponent<RectTransform>();
            targetRect.anchorMin = Vector2.zero;
            targetRect.anchorMax = Vector2.one;
            targetRect.offsetMin = Vector2.zero;
            targetRect.offsetMax = Vector2.zero;
        }

        private static Rect GetSafeArea()
        {
            var unitySafeArea = Screen.safeArea;
#if UNITY_IOS && !UNITY_EDITOR
            var iosSafeArea = GetIOSSafeArea();
            DebugLite.Log($"[SafeArea] Safe area requested ({unitySafeArea.ToString()}), iOS override: {iosSafeArea.ToString()}");
            return iosSafeArea;
#else
            DebugLite.Log($"[SafeArea] Safe area requested ({unitySafeArea.ToString()})");
            return unitySafeArea;
#endif
        }

#if UNITY_IOS
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SafeAreaData
        {
            public float top;
            public float bottom;
            public float left;
            public float right;
            public float width;
            public float height;
        }

        [DllImport("__Internal")]
        private static extern SafeAreaData GetIOSSafeAreaData();

        // Unity can sometimes return invalid SafeArea for iOS, so we need this hack to get the correct one
        private static Rect GetIOSSafeArea()
        {
            var data = GetIOSSafeAreaData();
            var widthMultiplier = Screen.width / data.width;
            var heightMultiplier = Screen.height / data.height;

            var rect = new Rect
            {
                xMin = data.left * widthMultiplier,
                yMin = data.bottom * heightMultiplier,
                xMax = Screen.width - (data.right * widthMultiplier),
                yMax = Screen.height - (data.top * heightMultiplier),
            };
            return rect;
        }
#endif
    }
}

