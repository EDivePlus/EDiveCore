// Author: František Holubec
// Created: 15.10.2026

using EDIVE.OdinExtensions.Attributes;
using EDIVE.Utils.Activations;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace EDIVE.UIElements
{
    // Copies a text to the clipboard, e.g. join code
    public class CopyTextToClipboard : MonoBehaviour
    {
        [SerializeReference]
        [EnhancedTypeSelector]
        private IActivation _Activation;

        [SerializeField]
        [Required]
        private TMP_Text _Source;

        private void OnEnable() => _Activation?.Register(Copy);
        private void OnDisable() => _Activation?.Unregister(Copy);

        public void Copy() => GUIUtility.systemCopyBuffer = _Source.text;
    }
}
