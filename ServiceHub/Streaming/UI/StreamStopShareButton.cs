// Author: František Holubec
// Created: 21.10.2026

using EDIVE.Core;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.ServiceHub.Streaming.UI
{
    // Stops sharing my view, e.g. from the watching toast
    [RequireComponent(typeof(Button))]
    public class StreamStopShareButton : MonoBehaviour
    {
        private Button _button;

        private void Awake() => _button = GetComponent<Button>();
        private void OnEnable() => _button.onClick.AddListener(OnClicked);
        private void OnDisable() => _button.onClick.RemoveListener(OnClicked);

        private static void OnClicked()
        {
            if (AppCore.Services.TryGet<StreamingManager>(out var streaming))
                streaming.ViewShare?.Stop();
        }
    }
}
