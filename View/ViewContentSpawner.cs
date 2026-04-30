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

namespace EDIVE.View
{
    [Serializable]
    [MovedFrom(true, "EDIVE.XRTools.Tablet", "EDIVE.XRTools", "TabletContentSpawner" )]
    public class ViewContentSpawner : ILoadable
    {
        [SerializeField]
        private RectTransform _Panel;

        [SerializeField]
        private VariableField<Transform> _SpawnRoot;

        [SerializeField]
        private bool _SetOrder;
        
        [ShowIf(nameof(_SetOrder))]
        [SerializeField]
        private int _Order;
        
        public UniTask Load(Action<float> progressCallback)
        {
            var panelInstance = Object.Instantiate(_Panel, _SpawnRoot.Value);
            panelInstance.SetToFillParent();
            if (_SetOrder)
                panelInstance.SetSiblingIndex(_Order);
            
            return UniTask.CompletedTask;
        }
    }
}
