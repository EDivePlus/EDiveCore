using UnityEngine.AddressableAssets;

namespace EDIVE.MenuScreen
{
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

    public class ReferenceViewSource : IViewSource
    {
        public AssetReferenceGameObject Reference { get; }

        public ReferenceViewSource(AssetReferenceGameObject reference)
        {
            Reference = reference;
        }
        
        public static implicit operator ReferenceViewSource(AssetReferenceGameObject reference) => new(reference);

        protected bool Equals(ReferenceViewSource other)
        {
            return Equals(Reference, other.Reference);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ReferenceViewSource) obj);
        }

        public override int GetHashCode()
        {
            return (Reference != null ? Reference.GetHashCode() : 0);
        }
    }
}

