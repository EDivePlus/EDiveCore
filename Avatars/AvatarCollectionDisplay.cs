// Author: František Holubec
// Created: 10.11.2025

using EDIVE.NativeUtils;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace EDIVE.Avatars
{
    public class AvatarCollectionDisplay : MonoBehaviour
    {
        [SerializeField]
        private bool _PopulateOnAwake = true;
        
        [SerializeField]
        private AvatarDefinitionTranslator _Translator;
        
        [SerializeField]
        private AvatarDisplay _AvatarDisplayPrefab;
        
        [SerializeField]
        private Transform _Container;
        
        private void Awake()
        {
            if (_PopulateOnAwake)
                Populate();
        }
        
        [Button]
        public void Populate()
        {
            _Container.DestroyChildren();
            foreach (var definition in _Translator.Definitions)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    var displayPrefab = ((AvatarDisplay) PrefabUtility.InstantiatePrefab(_AvatarDisplayPrefab, _Container));
                    displayPrefab.SetDefinition(definition);
                    continue;
                }
#endif
                var display = Instantiate(_AvatarDisplayPrefab, _Container);
                display.SetDefinition(definition);
            }
        }
    }
}
