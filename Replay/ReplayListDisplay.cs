// Author: Radim Holub
// Created: 04.12.2025

using System.IO;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Replay
{
    public class ReplayListDisplay : MonoBehaviour
    {
        [SerializeField] private ReplayController _ReplayController;
        [SerializeField] private ReplayListElementDisplay _ElementPrefab;
        [SerializeField] private Transform _ContentRoot;
        
        private void OnEnable()
        {
            if (_ReplayController != null)
                _ReplayController.RecordingSaved += OnRecordingSaved;

            RefreshList();
        }

        private void OnDisable()
        {
            if (_ReplayController != null)
                _ReplayController.RecordingSaved -= OnRecordingSaved;
        }

        private void OnRecordingSaved(string path)
        {
            RefreshList();
        }

        [Button]
        private void RefreshList()
        {
            var dir = ReplayUtils.RecordingsFolderPath;
            if (!Directory.Exists(dir))
                return;

            var files = Directory.GetFiles(dir, "*.dat");

            foreach (var file in files)
            {
                var element = Instantiate(_ElementPrefab, _ContentRoot);
                element.SetReplay(file, _ReplayController);
            }
        }
    }
}
