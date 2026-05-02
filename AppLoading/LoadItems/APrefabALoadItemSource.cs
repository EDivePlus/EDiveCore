using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading.Loadables;
using EDIVE.AppLoading.Utils;
using EDIVE.Core;
using EDIVE.DataStructures;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EDIVE.AppLoading.LoadItems
{
    [Serializable]
    public abstract class APrefabALoadItemSource : ALoadItemSource
    {
        [PropertyOrder(10)]
        [SerializeField]
        private bool _ApplyTransform;

        [PropertyOrder(10)]
        [ShowIf(nameof(_ApplyTransform))]
        [SerializeField]
        private TransformSnapshot _Transform;

        public override async UniTask LoadContent(LoadItemDefinition definition, Action<float> progressCallback)
        {
            var instance = await CreateInstance();
            if (instance == null)
            {
                DebugLite.LogError($"Load item '{definition.name}' instance is null", definition);
                return;
            }

            instance.name = instance.name.Replace("(Clone)", "");
            var progressDict = new Dictionary<ILoadable, float>();
            var loadableComponents = instance.GetLoadableComponents<ILoadable>();
            await UniTask.WhenAll(loadableComponents.Select(c => c.Load(progress => UpdateProgress(c, progress))));
            return;

            void UpdateProgress(ILoadable target, float progress)
            {
                progressDict[target] = progress;
                progressCallback.Invoke(progressDict.Values.Average());
            }
        }

        protected abstract UniTask<GameObject> CreateInstance();

        protected void OnInstantiated(GameObject instance)
        {
            if (instance.scene != AppCore.Instance.RootScene)
                SceneManager.MoveGameObjectToScene(instance, AppCore.Instance.RootScene);

            if (_ApplyTransform && instance != null)
                _Transform.ApplyTo(instance.transform);
        }

#if UNITY_EDITOR
        public override IEnumerable<Type> GetTypeDependencies()
        {
            if (!TryGetEditorPrefab(out var prefab) || prefab == null)
                return Enumerable.Empty<Type>();
            
            return prefab.GetLoadableComponents<IDependencyOwner>()
                .SelectMany(l => l.GetDependencies())
                .Distinct();
        }

        public override IEnumerable<Type> GetRepresentedTypes()
        {
            if (!TryGetEditorPrefab(out var prefab) || prefab == null)
                return Enumerable.Empty<Type>();

            var result = new List<Type>();
            result.AddRange(prefab.GetLoadableComponents<MonoBehaviour>()
                .Where(c => c != null)
                .Select(c => c.GetType()));

            result.AddRange(prefab.GetLoadableComponents<IDependencyRepresentative>()
                .Where(c => c != null)
                .SelectMany(c => c.GetRepresentedTypes()));

            return result;
        }

        protected abstract bool TryGetEditorPrefab(out GameObject prefab);
#endif
    }
}
