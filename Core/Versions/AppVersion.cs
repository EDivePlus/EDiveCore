using System;
using System.Globalization;
using Newtonsoft.Json;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
#endif

namespace EDIVE.Core.Versions
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public struct AppVersion : IComparable<AppVersion>, IEquatable<AppVersion>
    {
        public static readonly AppVersion ZERO = new(0);

        [SerializeField]
        [JsonProperty("Major")]
        private int _Major;

        [SerializeField]
        [JsonProperty("Minor")]
        private int _Minor;

        [SerializeField]
        [JsonProperty("Patch")]
        private int _Patch;

        [SerializeField]
        [JsonProperty("Build")]
        private int _Build;

        public int Major
        {
            get => _Major;
            set => _Major = AppVersionSignificanceUtils.ClampSegment(value);
        }
        public int Minor
        {
            get => _Minor;
            set => _Minor = AppVersionSignificanceUtils.ClampSegment(value);
        }
        public int Patch
        {
            get => _Patch;
            set => _Patch = AppVersionSignificanceUtils.ClampSegment(value);
        }
        public int Build
        {
            get => _Build;
            set => _Build = AppVersionSignificanceUtils.ClampSegment(value);
        }

        public AppVersion(int major = 0, int minor = 0, int patch = 0, int build = 0)
        {
            _Major = AppVersionSignificanceUtils.ClampSegment(major);
            _Minor = AppVersionSignificanceUtils.ClampSegment(minor);
            _Patch = AppVersionSignificanceUtils.ClampSegment(patch);
            _Build = AppVersionSignificanceUtils.ClampSegment(build);
        }

        public int GetSegment(AppVersionSignificance significance)
        {
            return significance switch
            {
                AppVersionSignificance.Major => Major,
                AppVersionSignificance.Minor => Minor,
                AppVersionSignificance.Patch => Patch,
                AppVersionSignificance.Build => Build,
                _ => throw new ArgumentOutOfRangeException(nameof(significance), significance, null)
            };
        }

        public void SetSegment(AppVersionSignificance significance, int value)
        {
            switch (significance)
            {
                case AppVersionSignificance.Major:
                    Major = value;
                    break;
                case AppVersionSignificance.Minor:
                    Minor = value;
                    break;
                case AppVersionSignificance.Patch:
                    Patch = value;
                    break;
                case AppVersionSignificance.Build:
                    Build = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(significance), significance, null);
            }
        }

        public AppVersion Incremented(AppVersionSignificance significance)
        {
            var result = this;
            result.SetSegment(significance, result.GetSegment(significance) + 1);
            for (var i = (int) significance + 1; i < AppVersionSignificanceUtils.SEGMENT_COUNT; i++)
                result.SetSegment(AppVersionSignificanceUtils.ALL[i], 0);
            return result;
        }

        public static bool TryParse(string versionString, out AppVersion version)
        {
            version = ZERO;

            var remaining = versionString.AsSpan();
            while (!remaining.IsEmpty && !char.IsDigit(remaining[0]))
                remaining = remaining[1..];

            var parsed = new AppVersion();
            foreach (var significance in AppVersionSignificanceUtils.ALL)
            {
                var dotIndex = remaining.IndexOf('.');
                var segment = dotIndex < 0 ? remaining : remaining[..dotIndex];

                if (!int.TryParse(segment, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
                    return false;

                parsed.SetSegment(significance, value);

                if (dotIndex < 0)
                {
                    version = parsed;
                    return true;
                }

                remaining = remaining[(dotIndex + 1)..];
            }

            return false;
        }

        public override string ToString() => string.Join(".", _Major, _Minor, _Patch, _Build);

        public string ToStoreString() => string.Join(".", _Major, _Minor, _Patch);

        public int CompareTo(AppVersion other) => CompareTo(other, AppVersionSignificance.Build);

        public int CompareTo(AppVersion other, AppVersionSignificance mostSpecificVersionSignificance)
        {
            var majorComparison = Major.CompareTo(other.Major);
            if (mostSpecificVersionSignificance == AppVersionSignificance.Major || majorComparison != 0) return majorComparison;
            var minorComparison = Minor.CompareTo(other.Minor);
            if (mostSpecificVersionSignificance == AppVersionSignificance.Minor || minorComparison != 0) return minorComparison;
            var patchComparison = Patch.CompareTo(other.Patch);
            if (mostSpecificVersionSignificance == AppVersionSignificance.Patch || patchComparison != 0) return patchComparison;
            return Build.CompareTo(other.Build);
        }

        public static bool operator <(AppVersion a, AppVersion b) { return a.CompareTo(b) < 0; }
        public static bool operator >(AppVersion a, AppVersion b) { return a.CompareTo(b) > 0; }

        public static bool operator <=(AppVersion a, AppVersion b) { return a.CompareTo(b) <= 0; }
        public static bool operator >=(AppVersion a, AppVersion b) { return a.CompareTo(b) >= 0; }

        public static bool operator ==(AppVersion a, AppVersion b) { return a.Equals(b); }
        public static bool operator !=(AppVersion a, AppVersion b) { return !a.Equals(b); }

        public bool Equals(AppVersion other)
        {
            return _Major == other._Major && _Minor == other._Minor && _Patch == other._Patch && _Build == other._Build;
        }

        public override bool Equals(object obj)
        {
            return obj is AppVersion other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_Major, _Minor, _Patch, _Build);
        }
    }

#if UNITY_EDITOR
    public class AppVersionDrawer : OdinValueDrawer<AppVersion>
    {
        private const float SEPARATOR_WIDTH = 6;
        private const float SUFFIX_MIN_WIDTH = 42;

        protected override void DrawPropertyLayout(GUIContent label)
        {
            SirenixEditorGUI.BeginHorizontalPropertyLayout(label);
            var version = ValueEntry.SmartValue;
            for (var i = 0; i < AppVersionSignificanceUtils.ALL.Length; i++)
            {
                if (i > 0) 
                    GUILayout.Label(".", SirenixGUIStyles.LabelCentered, GUILayout.Width(SEPARATOR_WIDTH));

                var significance = AppVersionSignificanceUtils.ALL[i];

                EditorGUI.BeginChangeCheck();
                var value = SirenixEditorFields.IntField(version.GetSegment(significance));
                if (EditorGUI.EndChangeCheck())
                {
                    version.SetSegment(significance, value);
                    ValueEntry.SmartValue = version;
                }

                var fieldRect = GUILayoutUtility.GetLastRect();
                if (fieldRect.width >= SUFFIX_MIN_WIDTH)
                    GUI.Label(fieldRect, AppVersionSignificanceUtils.GetLabel(significance), SirenixGUIStyles.RightAlignedGreyMiniLabel);
            }

            SirenixEditorGUI.EndHorizontalPropertyLayout();
        }
    }
#endif
}
