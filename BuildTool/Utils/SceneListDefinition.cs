// Author: František Holubec
// Created: 20.03.2025

using System.Collections.Generic;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.BuildTool.Utils
{
    public class SceneListDefinition : ScriptableObject
    {
        [SceneReference(SceneReferenceType.Path, true)]
        [ListDrawerSettings(ShowFoldout = false)]
        [SerializeField]
        private List<string> _Scenes = new();
        public List<string> Scenes => _Scenes;

        public static implicit operator List<string>(SceneListDefinition sceneList)
        {
            return sceneList.Scenes;
        }

        public static implicit operator string[](SceneListDefinition sceneList)
        {
            return sceneList.Scenes.ToArray();
        }
    }
}
