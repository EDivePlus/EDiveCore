// Author: František Holubec
// Created: 11.04.2025

using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EDIVE.SceneManagement
{
    public class DirectSceneInstance : ASceneInstance<DirectSceneDefinition>
    {
        public override Scene Scene => _scene;
        private Scene _scene;
        
        public DirectSceneInstance(DirectSceneDefinition definition) : base(definition) { }
        
        protected override async UniTask LoadOperation()
        { 
            await SceneManager.LoadSceneAsync(Definition.SceneAsset, LoadSceneMode.Additive);
            _scene = SceneManager.GetSceneByPath(Definition.SceneAsset);
        }

        protected override async UniTask UnloadOperation()
        {
            await SceneManager.UnloadSceneAsync(_scene);
        }
    }
}
