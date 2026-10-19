// Author: František Holubec
// Created: 27.08.2025

using System;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading.Loadables;
using EDIVE.DataStructures.VariableFields;
using EDIVE.NativeUtils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Object = UnityEngine.Object;
using UnityEngine.Serialization;

namespace EDIVE.View
{
    [Serializable]
    [MovedFrom(true, "EDIVE.XRTools.Tablet", "EDIVE.XRTools", "TabletContentSpawner" )]
    public class ViewContentSpawner : ILoadable
    {
        [SerializeField]
        [FormerlySerializedAs("_Panel")]
        private RectTransform _PanelPrefab;

        [SerializeField]
        private VariableField<Transform> _SpawnRoot;

        [SerializeField]
        [FormerlySerializedAs("_SetOrder")]
        private bool _OverrideSiblingIndex;
        
        [ShowIf(nameof(_OverrideSiblingIndex))]
        [SerializeField]
        [FormerlySerializedAs("_Order")]
        private int _SiblingIndex;
        
        public UniTask Load(Action<float> progressCallback)
        {
            var targetParent = _SpawnRoot.Value;
            if (targetParent == null)
            {
                Debug.LogError("Spawn root is null");
                return UniTask.CompletedTask;
            }
            
            var panelInstance = Object.Instantiate(_PanelPrefab, _SpawnRoot.Value);
            panelInstance.SetToFillParent();
            if (_OverrideSiblingIndex)
                panelInstance.SetSiblingIndex(_SiblingIndex);
            
            return UniTask.CompletedTask;
        }
    }
}
