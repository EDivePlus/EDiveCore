// Author: František Holubec
// Created: 11.04.2025

#if ADDRESSABLES
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace EDIVE.SceneManagement
{
    public class AddressableSceneInstance : ASceneInstance<IAddressableSceneDefinition>
    {
        public override Scene Scene => _sceneInstance.Scene;

        private SceneInstance _sceneInstance;
      
        public AddressableSceneInstance(IAddressableSceneDefinition definition) : base(definition) { }

        protected override async UniTask LoadOperation()
        {
            _sceneInstance = await Addressables.LoadSceneAsync(Definition.SceneReference, LoadSceneMode.Additive).Task;
        }

        protected override async UniTask UnloadOperation()
        { 
            await Addressables.UnloadSceneAsync(_sceneInstance).Task;
        }
    }
}
#endif
