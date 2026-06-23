using System;
using EDIVE.OdinExtensions.Attributes;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace EDIVE.MenuScreen
{
    [EnhancedTypeSelector(true, 1)]
    public interface IViewSource { }
    
    public class InstanceViewSource : IViewSource
    {
        public WidgetView Instance { get; }

        public InstanceViewSource(WidgetView instance)
        {
            Instance = instance;
        }
        
        public static implicit operator InstanceViewSource(WidgetView instance) => new(instance);

        protected bool Equals(InstanceViewSource other)
        {
            return Equals(Instance, other.Instance);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((InstanceViewSource) obj);
        }

        public override int GetHashCode()
        {
            return Instance != null ? Instance.GetHashCode() : 0;
        }
    }
    
    [Serializable]
    public class PrefabViewSource : IViewSource
    {
        [SerializeField]
        private GameObject _Prefab;
        public GameObject Prefab { get => _Prefab; internal set => _Prefab = value; }

        public PrefabViewSource() { }
        public PrefabViewSource(GameObject prefab)
        {
            _Prefab = prefab;
        }
        
        public static implicit operator PrefabViewSource(GameObject prefab) => new(prefab);

        protected bool Equals(PrefabViewSource other)
        {
            return Equals(Prefab, other.Prefab);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((PrefabViewSource) obj);
        }

        public override int GetHashCode()
        {
            return Prefab != null ? Prefab.GetHashCode() : 0;
        }
    }

    [Serializable]
    public class AddressablePrefabViewSource : IViewSource
    {
        [SerializeField]
        private AssetReferenceGameObject _Reference;
        public AssetReferenceGameObject Reference { get => _Reference; internal set => _Reference = value; }

        public AddressablePrefabViewSource() { }
        public AddressablePrefabViewSource(AssetReferenceGameObject reference)
        {
            _Reference = reference;
        }
        
        public static implicit operator AddressablePrefabViewSource(AssetReferenceGameObject reference) => new(reference);

        protected bool Equals(AddressablePrefabViewSource other)
        {
            return Equals(Reference, other.Reference);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((AddressablePrefabViewSource) obj);
        }

        public override int GetHashCode()
        {
            return Reference != null ? Reference.GetHashCode() : 0;
        }
    }
}

