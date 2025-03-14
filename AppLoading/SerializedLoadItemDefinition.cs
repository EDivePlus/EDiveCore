using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using EDIVE.OdinExtensions.Attributes;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.AppLoading
{
    public class SerializedLoadItemDefinition : ALoadItemDefinition
    {
        [PropertyOrder(20)]
        [SerializeReference]
        [HideLabel]
        [InlineProperty]
        [EnhancedBoxGroup("Target", SpaceBefore = 6)]
        [TypeSelectorSettings(FilterTypesFunction = "FilterTargetTypes")]
        private ILoadable _Target;

        public override bool IsValid => _Target != null;

        public override async UniTask LoadContent(Action<float> progressCallback) { await _Target.Load(progressCallback); }

#if UNITY_EDITOR
        public override IEnumerable<Type> GetTypeDependencies()
        {
            if (_Target is IDependencyOwner dependencyOwner)
                return dependencyOwner.GetDependencies().Distinct();
            return Enumerable.Empty<Type>();
        }

        public override IEnumerable<Type> GetRepresentedTypes()
        {
            if (_Target == null) yield break;
            yield return _Target.GetType();

            if (_Target is IDependencyRepresentative dependencyRepresentative)
            {
                foreach (var representedType in dependencyRepresentative.GetRepresentedTypes())
                {
                    yield return representedType;
                }
            }
        }

        [UsedImplicitly]
        public bool FilterTargetTypes(Type type) { return !typeof(Object).IsAssignableFrom(type); }
#endif
    }
}
