// Author: František Holubec
// Created: 26.02.2026

using System.Collections.Generic;
using EDIVE.NativeUtils;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace EDIVE.Environment.SceneSetup
{
    public class SceneSetupCollectionController : MonoBehaviour
    {
        [SerializeField]
        private List<SceneSetupDefinition> _Collection;
        
        [SerializeField]
        private SceneSetupSelector _DisplayPrefab;
        
        [SerializeField]
        private Transform _Container;
        
        [Button]
        public void Populate()
        {
            _Container.DestroyChildren();
            foreach (var definition in _Collection)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    var displayPrefab = (SceneSetupSelector) PrefabUtility.InstantiatePrefab(_DisplayPrefab, _Container);
                    displayPrefab.SetDefinition(definition);
                    continue;
                }
#endif
                var display = Instantiate(_DisplayPrefab, _Container);
                display.SetDefinition(definition);
            }
        }
    }
}
