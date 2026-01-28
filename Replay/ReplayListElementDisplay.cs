// Author: Radim Holub
// Created: 04.12.2025
using System.IO;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.Replay
{
    public class ReplayListElementDisplay : MonoBehaviour
    {
        [SerializeField] private TMP_Text _FileNameText;
        [SerializeField] private Button _LoadButton;

        private string _filePath;
        private ReplayController _replayController;

        public void SetReplay(string filePath, ReplayController controller)
        {
            _filePath = filePath;
            _replayController = controller;

            if (_FileNameText)
                _FileNameText.text = Path.GetFileNameWithoutExtension(filePath);
            
            if (_LoadButton)
            {
                _LoadButton.onClick.RemoveAllListeners();
                _LoadButton.onClick.AddListener(OnLoadClicked);
            }
        }

        private void OnLoadClicked()
        {
            if (_replayController == null || string.IsNullOrEmpty(_filePath))
                return;

            UniTask.Void(async () =>
            {
                var ok = await _replayController.LoadRecordingFromFileAsync(_filePath, () =>
                {
                    _replayController.StartPlayback();
                });

                if (!ok)
                    Debug.LogWarning($"Failed to load replay: {_filePath}");
            });
        }
    }
}
