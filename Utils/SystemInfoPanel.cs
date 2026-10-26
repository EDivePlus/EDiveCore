// Author: František Holubec
// Created: 26.10.2026

using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace EDIVE.Utils.SystemInformation
{
    public enum SystemInfoKind
    {
        Version,
        AppId,
        Model,
        OperatingSystem,
        Memory,
        Graphics,
        Display
    }

    // Short app and device info with copy and native share, full details are in the debug menu
    public class SystemInfoPanel : MonoBehaviour
    {
        [Serializable]
        private class Row
        {
            [SerializeField]
            private SystemInfoKind _Kind;

            [SerializeField]
            private TMP_Text _Label;

            [SerializeField]
            private TMP_Text _Value;

            public SystemInfoKind Kind => _Kind;
            public string Label => _Label ? _Label.text : _Kind.ToString();

            public void SetValue(string value)
            {
                if (_Value) _Value.text = value;
            }
        }

        [SerializeField]
        private List<Row> _Rows = new();

        [SerializeField]
        private Button _CopyButton;

        [Tooltip("Hidden where native share is not available")]
        [SerializeField]
        private Button _ShareButton;

        [SerializeField]
        private GameObject _CopyIcon;

        [Tooltip("Replaces the copy icon for a moment after copying")]
        [SerializeField]
        private GameObject _CopiedIcon;

        [SerializeField]
        private float _CopiedDuration = 1.5f;

        private float _copiedEndTime;
        private bool _copied;

        private static bool IsShareAvailable
        {
            get
            {
#if NATIVE_SHARE && (UNITY_ANDROID || UNITY_IOS)
                return true;
#else
                return false;
#endif
            }
        }

        private void Awake()
        {
            if (_CopyButton) _CopyButton.onClick.AddListener(Copy);
            if (_ShareButton)
            {
                _ShareButton.onClick.AddListener(Share);
                _ShareButton.gameObject.SetActive(IsShareAvailable);
            }
        }

        private void OnDestroy()
        {
            if (_CopyButton) _CopyButton.onClick.RemoveListener(Copy);
            if (_ShareButton) _ShareButton.onClick.RemoveListener(Share);
        }

        private void OnEnable()
        {
            foreach (var row in _Rows)
                row.SetValue(GetValue(row.Kind));
            SetCopied(false);
        }

        private void Update()
        {
            if (_copied && Time.unscaledTime >= _copiedEndTime)
                SetCopied(false);
        }

        public void Copy()
        {
            GUIUtility.systemCopyBuffer = CreateReport();
            _copiedEndTime = Time.unscaledTime + _CopiedDuration;
            SetCopied(true);
        }

        private void SetCopied(bool copied)
        {
            _copied = copied;
            if (_CopyIcon) _CopyIcon.SetActive(!copied);
            if (_CopiedIcon) _CopiedIcon.SetActive(copied);
        }

        public void Share()
        {
#if NATIVE_SHARE && (UNITY_ANDROID || UNITY_IOS)
            new NativeShare().SetSubject(Application.productName).SetTitle(Application.productName).SetText(CreateReport()).Share();
#endif
        }

        private string CreateReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine(Application.productName);
            foreach (var row in _Rows)
                sb.Append(row.Label).Append(": ").AppendLine(GetValue(row.Kind));
            return sb.ToString();
        }

        public static string GetValue(SystemInfoKind kind) => kind switch
        {
            SystemInfoKind.Version => Application.version,
            SystemInfoKind.AppId => Application.identifier,
            SystemInfoKind.Model => SystemInfo.deviceModel,
            SystemInfoKind.OperatingSystem => SystemInfo.operatingSystem,
            SystemInfoKind.Memory => $"{Mathf.RoundToInt(SystemInfo.systemMemorySize / 1024f)} GB",
            SystemInfoKind.Graphics => $"{SystemInfo.graphicsDeviceName} · {SystemInfo.graphicsDeviceType}",
            SystemInfoKind.Display => GetDisplay(),
            _ => string.Empty
        };

        // Per eye size on a headset
        private static string GetDisplay()
        {
            if (XRSettings.isDeviceActive)
                return $"{XRSettings.eyeTextureWidth} × {XRSettings.eyeTextureHeight} · {XRDevice.refreshRate:0} Hz";
            return $"{Screen.width} × {Screen.height} · {Screen.currentResolution.refreshRateRatio.value:0} Hz";
        }
    }
}
