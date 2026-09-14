using UnityEngine;

namespace EDIVE.Core.Versions
{
    public enum AppVersionSignificance
    {
        Major = 0,
        Minor = 1,
        Patch = 2,
        Build = 3
    }

    public static class AppVersionSignificanceUtils
    {
        public const int SEGMENT_COUNT = 4;
        public const int MAX_SEGMENT_DIGITS = 3;
        public const int MAX_SEGMENT_VALUE = 999;

        public static readonly AppVersionSignificance[] ALL =
        {
            AppVersionSignificance.Major,
            AppVersionSignificance.Minor,
            AppVersionSignificance.Patch,
            AppVersionSignificance.Build
        };

        private static readonly string[] LABELS = {"Major", "Minor", "Patch", "Build"};

        public static int ClampSegment(int value) => Mathf.Clamp(value, 0, MAX_SEGMENT_VALUE);

        public static string GetLabel(int index) => index >= 0 && index < SEGMENT_COUNT ? LABELS[index] : "";

        public static string GetLabel(AppVersionSignificance significance) => GetLabel((int) significance);
    }
}
