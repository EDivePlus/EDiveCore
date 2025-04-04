using System;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Serialization;
using UnityEditor;

namespace EDIVE.OdinExtensions.Editor
{
    public static class PersistentContextUtils
    {
        public static GlobalPersistentContext<TValue> Get<TKey1, TValue>(TKey1 alphaKey, TValue defaultValue) => 
            PersistentContext.Get(GetHash(alphaKey), defaultValue);
        
        public static GlobalPersistentContext<TValue> Get<TKey1, TKey2, TValue>(TKey1 alphaKey, TKey2 betaKey, TValue defaultValue) => 
            PersistentContext.Get(GetHash(alphaKey), GetHash(betaKey), defaultValue);

        public static GlobalPersistentContext<TValue> Get<TKey1, TKey2, TKey3, TValue>(TKey1 alphaKey, TKey2 betaKey, TKey3 gammaKey, TValue defaultValue) => 
            PersistentContext.Get(GetHash(alphaKey), GetHash(betaKey), GetHash(gammaKey), defaultValue);

        public static GlobalPersistentContext<TValue> Get<TKey1, TKey2, TKey3, TKey4, TValue>(TKey1 alphaKey, TKey2 betaKey, TKey3 gammaKey, TKey4 deltaKey, TValue defaultValue) => 
            PersistentContext.Get(GetHash(alphaKey), GetHash(betaKey), GetHash(gammaKey), GetHash(deltaKey), defaultValue);

        private static int GetHash<TKey>(TKey key) => key is Type typeKey ? TwoWaySerializationBinder.Default.BindToName(typeKey).GetHashCode() : key.GetHashCode();
    }

    [Serializable]
    [InlineProperty]
    [HideReferenceObjectPicker]
    public sealed class ValuePersistentContext<TValue>
    {
        private GlobalPersistentContext<TValue> _context;

        public ValuePersistentContext(GlobalPersistentContext<TValue> context)
        {
            _context = context;
        }

        [ShowInInspector]
        [HideLabel]
        public TValue Value
        {
            get => _context != null ? _context.Value : default;
            set
            {
                if (_context != null) _context.Value = value;
            }
        }
    }

    [Serializable]
    [InlineProperty]
    [HideReferenceObjectPicker]
    public sealed class AssetPersistentContext<TValue> where TValue : UnityEngine.Object
    {
        private GlobalPersistentContext<string> _context;

        public AssetPersistentContext(GlobalPersistentContext<string> context)
        {
            _context = context;
        }

        [ShowInInspector]
        [HideLabel]
        public TValue Value
        {
            get => !string.IsNullOrEmpty(_context?.Value) ? AssetDatabase.LoadAssetAtPath<TValue>(AssetDatabase.GUIDToAssetPath(_context?.Value)) : null;
            set
            {
                if (_context != null) _context.Value = value != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value)) : null;
            }
        }
    }
}
