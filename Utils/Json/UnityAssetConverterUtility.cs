// Author: František Holubec
// Created: 26.03.2025

namespace EDIVE.Utils.Json
{
    public static class UnityAssetConverterUtility
    {
        public static bool IsDisabled { get; private set; }

        public static void DisableOnce()
        {
            IsDisabled = true;
        }

        public static bool CheckDisabledAndRestore()
        {
            if (!IsDisabled) return false;
            IsDisabled = false;
            return true;
        }
    }
}
