// Author: František Holubec
// Created: 26.02.2026

using System.Collections.Generic;
using System.Linq;
using EDIVE.AddressableAssets;

namespace EDIVE.Environment.SceneSetup
{
    public class SceneSetupCollectionController : AddressablePrefabListController<SceneSetupSelector, SceneSetupDefinition>
    {
        protected override IEnumerable<SceneSetupDefinition> OrderAssets(IEnumerable<SceneSetupDefinition> assets)
        {
            return assets.OrderBy(def => def.UniqueID);
        }
        
        protected override void PopulatePrefab(SceneSetupSelector prefab, SceneSetupDefinition asset)
        {
            prefab.SetDefinition(asset);
            prefab.name = _DisplayPrefab.name + "_" + asset.UniqueID;
        }
    }
}
