// Author: František Holubec
// Created: 09.10.2026

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.AddressableAssets;
using EDIVE.AssetTranslation;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.VisualPresets.Presets;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace EDIVE.View.Windows
{
    // UniqueID is the route ID for ScreenRoot.Navigate
    [CreateAssetMenu(menuName = "EDIVE/View/Window Definition", fileName = "WindowDefinition")]
    public class WindowDefinition : AUniqueDefinition
    {
        [SerializeField]
        [EnhancedBoxGroup("Source")]
        private AWindow _Prefab;

        [Tooltip("Used when no direct prefab is set")]
        [SerializeField]
        [EnhancedBoxGroup("Source")]
        [HideIf(nameof(_Prefab))]
        private AssetAddressReference<GameObject> _PrefabReference;

        [SerializeField]
        [Required]
        [EnhancedBoxGroup("Screen")]
        [FormerlySerializedAs("_Layer")]
        private WindowLayer _WindowLayer;

        [Tooltip("Windows below stay drawn but take no input")]
        [SerializeField]
        [EnhancedBoxGroup("Screen")]
        private bool _Modal;

        [SerializeField]
        [EnhancedBoxGroup("Screen")]
        [ShowIf(nameof(_Modal))]
        private bool _CloseOnBackdrop;

        [SerializeField]
        [EnhancedBoxGroup("Screen")]
        [FormerlySerializedAs("_CloseOnBack")]
        [FormerlySerializedAs("_Back")]
        private WindowBackAction _BackAction;

        [Tooltip("Windows below get hidden once this one is opened")]
        [SerializeField]
        [EnhancedBoxGroup("Screen")]
        [FormerlySerializedAs("_Opaque")]
        private bool _HideWindowsBelow;

        [SerializeField]
        [EnhancedBoxGroup("Screen")]
        [FormerlySerializedAs("_KeepAfterClose")]
        private bool _ReuseInstance;

        [Tooltip("Closes instead of minimizing, for windows without state worth keeping")]
        [SerializeField]
        [EnhancedBoxGroup("Screen")]
        private bool _CloseOnMinimize;

        [Tooltip("Opening it again: Multiple opens another, Single brings it to front, Replace reopens it")]
        [SerializeField]
        [EnhancedBoxGroup("Screen")]
        [FormerlySerializedAs("_InstancePolicy")]
        private WindowInstancePolicy _ReopenPolicy;

        [Tooltip("Name, icon, color and tooltip for the window header, dock and library")]
        [SerializeField]
        [EnhancedBoxGroup("Visual")]
        [HideLabel]
        private VisualPreset _Visual = new();

        public WindowLayer Layer => _WindowLayer;
        public bool Modal => _Modal;
        public bool CloseOnBackdrop => _Modal && _CloseOnBackdrop;
        public WindowBackAction Back => _BackAction;
        public VisualPreset Visual => _Visual;
        public bool Opaque => _HideWindowsBelow;
        public bool KeepAfterClose => _ReuseInstance;
        public bool CloseOnMinimize => _CloseOnMinimize;
        public WindowInstancePolicy InstancePolicy => _ReopenPolicy;

        private bool UsesReference => _Prefab == null && _PrefabReference != null && !string.IsNullOrEmpty(_PrefabReference.Address);

        // Instances holding the addressable prefab, released when none is left
        [NonSerialized]
        private int _referenceUsers;

        // Every successful load needs one ReleasePrefab
        public async UniTask<AWindow> LoadPrefabAsync(CancellationToken cancellationToken)
        {
            if (!UsesReference)
                return _Prefab;

            _referenceUsers++;
            try
            {
                var prefab = await _PrefabReference.LoadAsync().ToUniTask(cancellationToken: cancellationToken);
                var window = prefab != null ? prefab.GetComponent<AWindow>() : null;
                if (window == null)
                    ReleasePrefab();
                return window;
            }
            catch
            {
                ReleasePrefab();
                throw;
            }
        }

        public void ReleasePrefab()
        {
            if (!UsesReference || _referenceUsers <= 0)
                return;

            _referenceUsers--;
            if (_referenceUsers == 0)
                _PrefabReference.Release();
        }
    }
}
