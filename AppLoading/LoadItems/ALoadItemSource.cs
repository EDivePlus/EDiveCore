// Author: František Holubec
// Created: 02.05.2026

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace EDIVE.AppLoading.LoadItems
{
    [Serializable]
    public abstract class ALoadItemSource
    {
        public abstract bool IsValid { get; }
        public abstract UniTask LoadContent(LoadItemDefinition definition, Action<float> progressCallback);

#if UNITY_EDITOR
         public abstract IEnumerable<Type> GetTypeDependencies();
         public abstract IEnumerable<Type> GetRepresentedTypes();
        
#endif
    }
}
