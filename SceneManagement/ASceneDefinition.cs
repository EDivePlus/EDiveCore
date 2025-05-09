// Author: František Holubec
// Created: 08.04.2025

using System;
using EDIVE.AssetTranslation;
using Sirenix.OdinInspector;

namespace EDIVE.SceneManagement
{
    public abstract class ASceneDefinition : AUniqueDefinition
    {
        public abstract bool IsValid();
        public abstract ASceneInstance CreateInstance();

#if UNITY_EDITOR
        [BoxGroup("Debug")]
        [HideLabel]
        [InlineProperty]
        [DisableInEditorMode]
        [ShowInInspector]
        private ASceneInstance DebugInstance
        {
            get => _debugInstance ??= CreateInstance();
            set { }
        }

        [NonSerialized]
        private ASceneInstance _debugInstance;

        [BoxGroup("Debug")]
        [Button]
        private void Clear()
        {
            _debugInstance = CreateInstance();
        }
#endif
    }
}
