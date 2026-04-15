// Author: František Holubec
// Created: 14.04.2026
#if FISHNET
using System;
using FishNet.Serializing;

namespace EDIVE.AddressableAssets.Fishnet
{
    public static class AddressablesFishnetUtils
    {
        public static void CustomWriteAssetAddressReference<T>(this Writer writer, T value) where T : AssetAddressReference
        {
            writer.WriteString(value != null ? value.Address : string.Empty);
        }

        public static T CustomReadAssetAddressReference<T>(this Reader reader) where T : AssetAddressReference 
        {
            return Activator.CreateInstance(typeof(T), reader.ReadStringAllocated()) as T;
        }
        
        public static T CustomReadAssetAddressReference<T>(this Reader reader, Func<string, T> constructor) where T : AssetAddressReference 
        {
            return constructor?.Invoke(reader.ReadStringAllocated());
        }
    }
}
#endif
