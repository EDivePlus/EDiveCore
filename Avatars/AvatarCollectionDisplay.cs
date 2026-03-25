// Author: František Holubec
// Created: 10.11.2025

using System.Collections.Generic;
using System.Linq;
using EDIVE.AddressableAssets;

namespace EDIVE.Avatars
{
    public class AvatarCollectionDisplay : AddressablePrefabListController<AvatarDisplay, AvatarDefinition>
    {
        protected override IEnumerable<AvatarDefinition> OrderAssets(IEnumerable<AvatarDefinition> assets)
        {
            return assets.OrderBy(def => def.UniqueID);
        }
        
        protected override void PopulatePrefab(AvatarDisplay prefab, AvatarDefinition asset)
        {
            prefab.SetDefinition(asset);
            prefab.name = _DisplayPrefab.name + "_" + asset.UniqueID;
        }
    }
}
