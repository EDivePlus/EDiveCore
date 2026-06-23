#if ADDRESSABLES
using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using EDIVE.NativeUtils;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace EDIVE.AppLoading.Loadables
{
    [Serializable]
    public class AssetPreloadLoadable : ILoadable, IDependencyRepresentative
    {
        [SerializeField]
        private List<AssetReference> _Assets = new();

        public async UniTask Load(Action<float> progressCallback)
        {
            var valid = _Assets.Where(a => a != null && a.RuntimeKeyIsValid()).ToList();
            if (valid.Count == 0)
                return;

            var progress = new float[valid.Count];
            await UniTask.WhenAll(valid.Select((reference, index) => LoadSingle(reference, index)));
            return;

            async UniTask LoadSingle(AssetReference reference, int index)
            {
                var asset = await reference.LoadAssetAsync<Object>().ToUniTask(Progress.Create<float>(value =>
                {
                    progress[index] = value;
                    progressCallback?.Invoke(progress.Average());
                }));

                PreloadedAssets.Register(asset);
            }
        }

        public IEnumerable<Type> GetRepresentedTypes()
        {
#if UNITY_EDITOR
            foreach (var reference in _Assets)
            {
                if (reference is { editorAsset: { } asset })
                    yield return asset.GetType();
            }
#else
            yield break;
#endif
        }
    }
}
#endif
