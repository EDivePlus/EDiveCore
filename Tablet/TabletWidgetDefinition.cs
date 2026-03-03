// Author: Michal Petr
// Created: 03.03.2026

using EDIVE.AddressableAssets;
using EDIVE.AssetTranslation;
using EDIVE.VisualPresets.Presets;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace EDIVE.Tablet
{
    public class TabletWidgetDefinition : AUniqueDefinition
    {
        [SerializeField]
        private VisualPreset _Visual;
        
        [SerializeField]
        [AssetReferenceTypeRestriction(typeof(ATabletView))]
        private AssetReferenceGameObject _WidgetView;
        
        public VisualPreset Visual => _Visual;
        public AssetReferenceGameObject WidgetView => _WidgetView;
    }
}
