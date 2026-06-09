using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using EDIVE.EditorUtils;
#endif

namespace EDIVE.AppLoading.LoadItems.Legacy
{
    public class PrefabLoadItemDefinition : LoadItemDefinition, ISelfValidator
    {
    #region Migration
        [SerializeField, HideInInspector] private GameObject _Prefab;

        protected override void TryMigrate()
        {
            base.TryMigrate();
            if (_Prefab != null)
            {
                _Source = new PrefabLoadItemSource(_Prefab);
                _Prefab = null;
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
