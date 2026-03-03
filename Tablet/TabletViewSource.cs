using UnityEngine.AddressableAssets;

namespace EDIVE.Tablet
{
    public interface ITabletViewSource { }
    
    public class InstanceTabletViewSource : ITabletViewSource
    {
        public ATabletView Instance { get; }

        public InstanceTabletViewSource(ATabletView instance)
        {
            Instance = instance;
        }
        
        public static implicit operator InstanceTabletViewSource(ATabletView instance) => new(instance);

        protected bool Equals(InstanceTabletViewSource other)
        {
            return Equals(Instance, other.Instance);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((InstanceTabletViewSource) obj);
        }

        public override int GetHashCode()
        {
            return Instance != null ? Instance.GetHashCode() : 0;
        }
    }

    public class ReferenceTabletViewSource : ITabletViewSource
    {
        public AssetReferenceGameObject Reference { get; }

        public ReferenceTabletViewSource(AssetReferenceGameObject reference)
        {
            Reference = reference;
        }
        
        public static implicit operator ReferenceTabletViewSource(AssetReferenceGameObject reference) => new(reference);

        protected bool Equals(ReferenceTabletViewSource other)
        {
            return Equals(Reference, other.Reference);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ReferenceTabletViewSource) obj);
        }

        public override int GetHashCode()
        {
            return (Reference != null ? Reference.GetHashCode() : 0);
        }
    }
}

