#if ADDRESSABLES
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.AddressableAssets
{
    public class AssetReferenceTypeRestrictionAttribute : AssetReferenceUIRestriction
    {
        private readonly Type _type;
            
        public AssetReferenceTypeRestrictionAttribute(Type type)
        {
            _type = type;
        }
            
        public override bool ValidateAsset(Object obj)
        {
            if (obj == null)
                return true;
            if (obj is GameObject gameObj) 
                return _type == null || gameObj.GetComponent(_type) != null;
            return _type == null || _type.IsInstanceOfType(obj);
        }
    }
}
#endif
