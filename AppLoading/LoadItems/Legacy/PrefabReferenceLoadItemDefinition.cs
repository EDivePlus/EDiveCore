#if ADDRESSABLES

using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

#if UNITY_EDITOR
using EDIVE.EditorUtils;
#endif

namespace EDIVE.AppLoading.LoadItems.Legacy
{
    public class PrefabReferenceLoadItemDefinition : LoadItemDefinition, ISelfValidator
    {
    #region Migration
        [SerializeField, HideInInspector]  private AssetReferenceGameObject _PrefabReference;

        protected override void TryMigrate()
        {
            base.TryMigrate();
            if (_PrefabReference != null)
            {
                _Source = new PrefabReferenceLoadItemSource(_PrefabReference);
                _PrefabReference = null;
            }
        }
        
        public void Validate(SelfValidationResult result)
        {
#if UNITY_EDITOR
            result.AddError("PrefabLoadItemDefinition is obsolete, migrate to base class")
                .WithFix(() => this.ChangeType<LoadItemDefinition>());
#endif
        }
    #endregion
    }
}
#endif
