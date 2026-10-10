// Author: František Holubec
// Created: 10.10.2026

using UnityEngine;

#if UNITY_IOS
using System.Runtime.InteropServices;
#endif

namespace EDIVE.View
{
    public static class SafeAreaUtility
    {
        // Safe area in screen pixels, origin bottom left
        public static Rect GetSafeArea()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return GetIOSSafeArea();
#else
            return Screen.safeArea;
#endif
        }

#if UNITY_IOS
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct SafeAreaData
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
            if (data.width <= 0 || data.height <= 0)
                return Screen.safeArea;

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
