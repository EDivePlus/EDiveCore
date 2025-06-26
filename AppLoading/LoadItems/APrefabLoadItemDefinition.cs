using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading.Loadables;
using EDIVE.AppLoading.Utils;
using EDIVE.DataStructures;
using EDIVE.NativeUtils;
using UnityEngine;

namespace EDIVE.AppLoading.LoadItems
{
    public abstract class APrefabLoadItemDefinition : ALoadItemDefinition
    {
        [SerializeField]
        private TransformSnapshot _Transform;

        public override async UniTask LoadContent(Action<float> progressCallback)
        {
            var instance = await CreateInstance();
            if (instance == null)
            {
                Debug.LogError($"Load item '{name}' instance is null", this);
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

        protected void ApplyTransform(GameObject instance)
        {
            if (instance == null) return;
            _Transform.ApplyTo(instance.transform);
        }

#if UNITY_EDITOR
        public abstract GameObject EditorPrefab { get; }

        public override IEnumerable<Type> GetTypeDependencies()
        {
            return GetAvailableEditorInstances().EmptyIfNull()
                .SelectMany(i => i.GetLoadableComponents<IDependencyOwner>().SelectMany(l => l.GetDependencies()))
                .Distinct();
        }

        public override IEnumerable<Type> GetRepresentedTypes()
        {
            var instances = GetAvailableEditorInstances().EmptyIfNull();
            var result = new List<Type>();
            foreach (var instance in instances)
            {
                if (instance == null) continue;
                result.AddRange(instance.GetLoadableComponents<MonoBehaviour>()
                    .Where(c => c != null).Select(c => c.GetType()));

                result.AddRange(instance.GetLoadableComponents<IDependencyRepresentative>()
                    .Where(c => c != null).SelectMany(c => c.GetRepresentedTypes()));
            }

            return result;
        }

        protected abstract IEnumerable<GameObject> GetAvailableEditorInstances();
#endif
    }
}
