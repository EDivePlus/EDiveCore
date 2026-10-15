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

        // Used when no direct prefab is set
        [SerializeField]
        [EnhancedBoxGroup("Source")]
        [HideIf(nameof(_Prefab))]
        private AssetAddressReference<GameObject> _PrefabReference;

        [SerializeField]
        [Required]
        [EnhancedBoxGroup("Screen")]
        private WindowLayer _Layer;

        // Windows below stay drawn but take no input
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
        private WindowBackAction _Back;

        // Windows below get hidden once this one is opened
        [SerializeField]
        [EnhancedBoxGroup("Screen")]
        private bool _Opaque;

        // Kept hidden after close and reused by the next open
        [SerializeField]
        [EnhancedBoxGroup("Screen")]
        private bool _KeepAfterClose;

        // Closes instead of minimizing, for windows without state worth keeping
        [SerializeField]
        [EnhancedBoxGroup("Screen")]
        private bool _CloseOnMinimize;

        [SerializeField]
        [EnhancedBoxGroup("Screen")]
        private WindowInstancePolicy _InstancePolicy;

        // Name, icon, color and tooltip for the window header, dock and library
        [SerializeField]
        [EnhancedBoxGroup("Visual")]
        [HideLabel]
        private VisualPreset _Visual = new();

        public WindowLayer Layer => _Layer;
        public bool Modal => _Modal;
        public bool CloseOnBackdrop => _Modal && _CloseOnBackdrop;
        public WindowBackAction Back => _Back;
        public VisualPreset Visual => _Visual;
        public bool Opaque => _Opaque;
        public bool KeepAfterClose => _KeepAfterClose;
        public bool CloseOnMinimize => _CloseOnMinimize;
        public WindowInstancePolicy InstancePolicy => _InstancePolicy;

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
