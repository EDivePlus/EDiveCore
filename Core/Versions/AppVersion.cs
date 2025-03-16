using System;
using System.Text;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EDIVE.Core.Versions
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class AppVersion : IComparable<AppVersion>, IEquatable<AppVersion>
    {
        public static readonly AppVersion ZERO = new();
        
        [Wrap(0, 999)]
        [HorizontalGroup("Version")]
        [SuffixLabel("Major", true)]
        [HideLabel]
        [SerializeField]
        [JsonProperty("Major")]
        private int _Major;
        
        [Wrap(0, 999)]
        [HorizontalGroup("Version")]
        [SuffixLabel("Minor", true)]
        [LabelWidth(8)]
        [LabelText(".")]
        [SerializeField]
        [JsonProperty("Minor")]
        private int _Minor;
        
        [Wrap(0, 999)]
        [HorizontalGroup("Version")]
        [SuffixLabel("Patch", true)]
        [LabelWidth(8)]
        [LabelText(".")]
        [SerializeField]
        [JsonProperty("Patch")]
        private int _Patch;
        
        [Wrap(0, 999)]
        [HorizontalGroup("Version")]
        [SuffixLabel("Build", true)]
        [LabelWidth(8)]
        [LabelText(".")]
        [SerializeField]
        [JsonProperty("Build")]
        private int _Build;

        public int Major
        {
            get => _Major;
            set => _Major = value;
        }
        public int Minor
        {
            get => _Minor;
            set => _Minor = value;
        }
        public int Patch
        {
            get => _Patch;
            set => _Patch = value;
        }
        public int Build
        {
            get => _Build;
            set => _Build = value;
        }

        public AppVersion(int major = 0, int minor = 0, int patch = 0, int build = 0)
        {
            _Major = major;
            _Minor = minor;
            _Patch = patch;
            _Build = build;
        }

        public AppVersion(AppVersion other)
        {
            _Major = other.Major;
            _Minor = other.Minor;
            _Patch = other.Patch;
            _Build = other.Build;
        }

        public AppVersion GetCopy() => new(this);

        public AppVersion WithMajor(int major) => new(major, Minor, Patch, Build);
        public AppVersion WithMinor(int minor) => new(Major, minor, Patch, Build);
        public AppVersion WithPatch(int patch) => new(Major, Minor, patch, Build);
        public AppVersion WithBuild(int build) => new(Major, Minor, Patch, build);
        
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

        public int CompareTo(AppVersion other)
        {
            var majorComparison = Major.CompareTo(other.Major);
            if (majorComparison != 0) return majorComparison;
            var minorComparison = Minor.CompareTo(other.Minor);
            if (minorComparison != 0) return minorComparison;
            var revisionComparison = Patch.CompareTo(other.Patch);
            if (revisionComparison != 0) return revisionComparison;
            return Build.CompareTo(other.Build);
        }

        [Button]
        public string GetString(AppVersionFormat format)
        {
            var stringBuilder = new StringBuilder(20);

            if (!string.IsNullOrEmpty(format.Prefix))
                stringBuilder.Append(format.Prefix);
            
            foreach (var significance in format.GetSignificances())
            {
                if (significance > AppVersionSignificance.Major) stringBuilder.Append('.');
                var leadingZeros = format.GetZerosAt(significance);
                var segmentValue = GetSegment(significance).ToString($"D{leadingZeros}");
                stringBuilder.Append(segmentValue);
            }
            return stringBuilder.ToString();
        }
        
        public int GetCode(BundleCodeFormat bundleFormat)
        {
            return Major * (int) Math.Pow(10, bundleFormat.MinorDigits + bundleFormat.PatchDigits) + Minor * (int) Math.Pow(10, bundleFormat.PatchDigits) + Patch;
        }
        
        public AppVersion FromCode(int code, BundleCodeFormat bundleFormat)
        {
            var minorPatchFactor = (int) Math.Pow(10, bundleFormat.PatchDigits);
            var majorFactor = (int) Math.Pow(10, bundleFormat.MinorDigits + bundleFormat.PatchDigits);

            var major = code / majorFactor;
            var remainder = code % majorFactor;
            var minor = remainder / minorPatchFactor;
            var patch = remainder % minorPatchFactor;
            return new AppVersion(major, minor, patch);
        }

        public static AppVersion FromBaseString(string versionString)
        {
            var parts = versionString.Split('.');
            var version = new AppVersion();
            if (parts.Length > 0) version.Major = int.Parse(parts[0]);
            if (parts.Length > 1) version.Minor = int.Parse(parts[1]);
            if (parts.Length > 2) version.Patch = int.Parse(parts[2]);
            if (parts.Length > 3) version.Build = int.Parse(parts[3]);
            return version;
        }

        public static bool operator <(AppVersion a, AppVersion b) { return a.CompareTo(b) < 0; }
        public static bool operator >(AppVersion a, AppVersion b) { return a.CompareTo(b) > 0; }

        public static bool operator <=(AppVersion a, AppVersion b) { return a.CompareTo(b) <= 0; }
        public static bool operator >=(AppVersion a, AppVersion b) { return a.CompareTo(b) >= 0; }

        public int CompareTo(AppVersion other, AppVersionSignificance mostSpecificVersionSignificance)
        {
            var majorComparison = Major.CompareTo(other.Major);
            if (mostSpecificVersionSignificance == AppVersionSignificance.Major || majorComparison != 0) return majorComparison;
            var minorComparison = Minor.CompareTo(other.Minor);
            if (mostSpecificVersionSignificance == AppVersionSignificance.Minor || minorComparison != 0) return minorComparison;
            var revisionComparison = Patch.CompareTo(other.Patch);
            if (mostSpecificVersionSignificance == AppVersionSignificance.Patch || revisionComparison != 0) return revisionComparison;
            return Build.CompareTo(other.Build);
        }

        public bool Equals(AppVersion other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return _Major == other._Major && _Minor == other._Minor && _Patch == other._Patch && _Build == other._Build;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((AppVersion) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Major;
                hashCode = (hashCode * 397) ^ Minor;
                hashCode = (hashCode * 397) ^ Patch;
                hashCode = (hashCode * 397) ^ Build;
                return hashCode;
            }
        }

        public string ToBaseString()
        {
            return string.Join(".", _Major.ToString(), _Minor.ToString(), _Patch.ToString(), _Build.ToString());
        }
        
#if UNITY_EDITOR
        public void ApplyVersion(AppVersionFormat format, BundleCodeFormat bundleFormat)
        {
            PlayerSettings.bundleVersion = GetString(format);
            PlayerSettings.Android.bundleVersionCode = GetCode(bundleFormat);
            PlayerSettings.iOS.buildNumber = Mathf.Max(0, Build).ToString();
        }
#endif

    }
}
