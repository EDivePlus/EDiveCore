// Author: František Holubec
// Created: 25.03.2026

using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.NativeUtils;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace EDIVE.AddressableAssets
{
    public abstract class AddressablePrefabListController<TPrefab, TAsset> : MonoBehaviour 
        where TPrefab : MonoBehaviour 
        where TAsset : Object
    {
        [SerializeField]
        private bool _PopulateOnAwake = true;

        [SerializeField]
        private AssetLabelReference _AssetLabel;

        [SerializeField]
        protected TPrefab _DisplayPrefab;

        [SerializeField]
        private Transform _Container;

        private AsyncOperationHandle<IList<TAsset>> _assetsHandle;

        private void Awake()
        {
            if (_PopulateOnAwake)
                Populate(destroyCancellationToken).Forget();
        }

        private void OnDestroy()
        {
            if (_assetsHandle.IsValid())
                _assetsHandle.Release();
        }

        private async UniTask Populate(CancellationToken cancellationToken = default)
        {
            if (!_AssetLabel.RuntimeKeyIsValid())
                return;

            _Container.DestroyChildren();
            try
            {
                _assetsHandle = Addressables.LoadAssetsAsync<TAsset>(_AssetLabel.RuntimeKey, null);
                var definitions = await _assetsHandle.WithCancellation(cancellationToken, false, true);
                
                foreach (var definition in OrderAssets(definitions))
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        var displayPrefab = (TPrefab) PrefabUtility.InstantiatePrefab(_DisplayPrefab, _Container);
                        if (displayPrefab)
                            PopulatePrefab(displayPrefab, definition);
                        continue;
                    }
#endif
                    var display = Instantiate(_DisplayPrefab, new InstantiateParameters
                    {
                        parent = _Container,
                        worldSpace = false
                    });
                    if (display)
                        PopulatePrefab(display, definition);
                }
            }
            catch (InvalidKeyException e)
            {
                // ignore
            }
        }

        [Button]
        private void TestPopulate()
        {
            Populate().Forget();
        }
        
        protected virtual IEnumerable<TAsset> OrderAssets(IEnumerable<TAsset> assets) => assets;
        protected abstract void PopulatePrefab(TPrefab prefab, TAsset asset);
    }
}
