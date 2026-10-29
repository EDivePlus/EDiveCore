// Author: František Holubec
// Created: 25.03.2026

using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.NativeUtils;
using Sirenix.OdinInspector;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif
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

        [SerializeField]
        [Tooltip("Optional, shows how many are listed, a dash while loading")]
        private TMP_Text _CountText;

        private AsyncOperationHandle<IList<TAsset>> _assetsHandle;

        private void Awake()
        {
            if (_PopulateOnAwake)
                Populate(destroyCancellationToken).Forget();
            else
                SetCountText(_Container.childCount.ToString());
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
            if (_assetsHandle.IsValid())
                _assetsHandle.Release();

            SetCountText("–");
            var count = 0;
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
                        count++;
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
                    count++;
                }
            }
            catch (InvalidKeyException)
            {
                // ignore
            }
            finally
            {
                if (this)
                    SetCountText(count.ToString());
            }
        }

        private void SetCountText(string text)
        {
            if (_CountText)
                _CountText.text = text;
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
