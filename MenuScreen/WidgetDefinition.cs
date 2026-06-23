// Author: Michal Petr
// Created: 03.03.2026

using EDIVE.AddressableAssets;
using EDIVE.AssetTranslation;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.VisualPresets.Presets;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace EDIVE.MenuScreen
{
    public class WidgetDefinition : AUniqueDefinition, ISerializationCallbackReceiver
    {
        [SerializeField]
        private VisualPreset _Visual;
        
        [SerializeField]
        [HideInInspector]
        private AssetReferenceGameObject _WidgetView;
        
        [SerializeReference]
        [EnhancedTypeSelector(true, 1, OnTypeChanged = "OnViewSourceChanged")]
        private IViewSource _ViewSource;
        
        public VisualPreset Visual => _Visual;
        // public AssetReferenceGameObject WidgetView => _WidgetView;
        public IViewSource ViewSource => _ViewSource;
        
        public void OnBeforeSerialize() { }
        public void OnAfterDeserialize()
        {
            if (_ViewSource == null && _WidgetView != null)
            {
                _ViewSource = new AddressablePrefabViewSource(_WidgetView);
                _WidgetView = null;
            }
        }

#if UNITY_EDITOR
        [UsedImplicitly]
        private void OnViewSourceChanged(object prevValue, object newValue)
        {
            if (prevValue is PrefabViewSource prefabSource && newValue is AddressablePrefabViewSource newReferenceSource)
            {
                var prefabReference = AddressablesEditorUtils.CreateReference(prefabSource.Prefab);
                newReferenceSource.Reference = prefabReference;
            }

            if (prevValue is AddressablePrefabViewSource referenceSource && newValue is PrefabViewSource newPrefabSource)
            {
                var prefab = referenceSource.Reference.editorAsset;
                newPrefabSource.Prefab = prefab;
            }
        }
#endif
    }
}
