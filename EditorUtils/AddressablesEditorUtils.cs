#if ADDRESSABLES
using System;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace EDIVE.EditorUtils
{
    public static class AddressablesEditorUtils
    {
        public static bool IsAddressable(string assetGuid)
        {
            while (true)
            {
                if (DetermineIsAssetAddressable(assetGuid)) return true;

                var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                if (string.IsNullOrEmpty(assetPath)) return false;

                var parentDir = Path.GetDirectoryName(assetPath);
                var parentGuid = AssetDatabase.AssetPathToGUID(parentDir);
                assetGuid = parentGuid;
            }
            
            bool DetermineIsAssetAddressable(string guid)
            {
                var addressablesSettings = AddressableAssetSettingsDefaultObject.Settings;
                foreach (var group in addressablesSettings.groups)
                {
                    if (group == null)
                        continue;
                
                    foreach (var entry in group.entries)
                    {
                        if (entry == null)
                            continue;
                    
                        if (entry.guid == guid)
                            return true;
                    }
                }

                return false;
            }
        }

        public static void AddToDefaultAddressableGroup(string assetGuid, string label = null)
        {
            var addressablesSettings = AddressableAssetSettingsDefaultObject.Settings;
            var defaultGroup = addressablesSettings.DefaultGroup;
            var entry = addressablesSettings.CreateOrMoveEntry(assetGuid, defaultGroup);
            if (!string.IsNullOrEmpty(label))
                entry.labels.Add(label);
            addressablesSettings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);

            AssetDatabase.SaveAssets();
        }

        public static AssetReferenceGameObject ConvertToReference(GameObject originalReference, string label = null)
        {
            if (originalReference == null || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(originalReference, out var guid, out  _)) 
                return null;
            
            var assetReference = new AssetReferenceGameObject(guid);
            try
            {
                if (!IsAddressable(guid))
                    AddToDefaultAddressableGroup(guid, label);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            return assetReference;
        }

        public static AssetReferenceGameObject ConvertToReference(MonoBehaviour originalReference, string label = null)
        {
            return originalReference != null ? ConvertToReference(originalReference.gameObject, label) : null;
        }
    }
}
#endif
