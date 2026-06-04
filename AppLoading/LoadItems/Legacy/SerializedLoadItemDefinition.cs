using EDIVE.AppLoading.Loadables;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using EDIVE.EditorUtils;
#endif

namespace EDIVE.AppLoading.LoadItems.Legacy
{
    public class SerializedLoadItemDefinition : LoadItemDefinition, ISelfValidator
    {
    #region Migration
        [SerializeReference, HideInInspector]  private ILoadable _Target;

        protected override void TryMigrate()
        {
            base.TryMigrate();
            if (_Target != null)
            {
                _Source = new SerializedLoadItemSource(_Target);
                _Target = null;
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
