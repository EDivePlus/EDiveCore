using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.AppLoading.Finalizers
{
    [Serializable]
    public class SequentialLoadFinalizer : ILoadFinalizer
    {
        [InlineProperty]
        [HideLabel]
        [SerializeReference]
        [ListDrawerSettings(ShowFoldout = false)]
        private List<ILoadFinalizer> _Finalizers;

        public virtual async UniTask<bool> TryFinalizeLoad()
        {
            if (_Finalizers == null)
                return false;

            foreach (var finalizer in _Finalizers)
            {
                if (finalizer != null && await finalizer.TryFinalizeLoad())
                    return true;
            }
            return false;
        }
    }
}
