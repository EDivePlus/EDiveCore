using System;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.DataStructures.RectTransformSnapshot
{
    [Serializable]
    [InlineProperty]
    [JsonObject(MemberSerialization.OptIn)]
    public class RectTransformSnapshot
    {
        [JsonProperty]
        [SerializeField]
        private Vector3 _AnchoredPosition;

        [JsonProperty]
        [SerializeField]
        private Vector2 _SizeDelta;

        [JsonProperty]
        [SerializeField]
        private Vector2 _AnchorMin;

        [JsonProperty]
        [SerializeField]
        private Vector2 _AnchorMax;

        [JsonProperty]
        [SerializeField]
        private Vector2 _Pivot;

        [JsonProperty]
        [SerializeField]
        private Quaternion _Rotation = Quaternion.identity;

        [JsonProperty]
        [SerializeField]
        private Vector3 _LocalScale = Vector3.one;

        public Vector3 AnchoredPosition
        {
            get => _AnchoredPosition;
            set => _AnchoredPosition = value;
        }

        public Vector2 SizeDelta
        {
            get => _SizeDelta;
            set => _SizeDelta = value;
        }

        public Vector2 AnchorMin
        {
            get => _AnchorMin;
            set => _AnchorMin = value;
        }

        public Vector2 AnchorMax
        {
            get => _AnchorMax;
            set => _AnchorMax = value;
        }

        public Vector2 Pivot
        {
            get => _Pivot;
            set => _Pivot = value;
        }

        public Quaternion Rotation
        {
            get => _Rotation;
            set => _Rotation = value;
        }

        public Vector3 LocalScale
        {
            get => _LocalScale;
            set => _LocalScale = value;
        }

        public RectTransformSnapshot() { }
        public RectTransformSnapshot(RectTransform transform)
        {
            FromRectTransform(transform);
        }
        
        public RectTransformSnapshot(Vector3 anchoredPosition, Quaternion rotation, Vector3 scale, Vector2 sizeDelta, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            _AnchoredPosition = anchoredPosition;
            _Rotation = rotation;
            _LocalScale = scale;
            _SizeDelta = sizeDelta;
            _AnchorMin = anchorMin;
            _AnchorMax = anchorMax;
            _Pivot = pivot;
        }
        
        public RectTransformSnapshot(Vector3 anchoredPosition, Quaternion rotation, Vector3 scale, Vector2 sizeDelta)
        {
            _AnchoredPosition = anchoredPosition;
            _Rotation = rotation;
            _LocalScale = scale;
            _SizeDelta = sizeDelta;
            _AnchorMin = Vector2.one / 2;
            _AnchorMax = Vector2.one / 2;
            _Pivot = Vector2.one / 2;
        }

        public void FromRectTransform(RectTransform transform)
        {
            _AnchoredPosition = transform.anchoredPosition;
            _Rotation = transform.rotation;
            _LocalScale = transform.localScale;
            _SizeDelta = transform.sizeDelta;
            _AnchorMin = transform.anchorMin;
            _AnchorMax = transform.anchorMax;
            _Pivot = transform.pivot;
        }

        public void ApplyTo(RectTransform transform)
        {
            transform.anchoredPosition = _AnchoredPosition;
            transform.rotation = _Rotation;
            transform.localScale = _LocalScale;
            transform.sizeDelta = _SizeDelta;
            transform.anchorMin = _AnchorMin;
            transform.anchorMax = _AnchorMax;
            transform.pivot = _Pivot;
        }

        protected bool Equals(RectTransformSnapshot other)
        {
            return _AnchoredPosition.Equals(other._AnchoredPosition) &&
                   _SizeDelta.Equals(other._SizeDelta) && 
                   _AnchorMin.Equals(other._AnchorMin) && 
                   _AnchorMax.Equals(other._AnchorMax) && 
                   _Pivot.Equals(other._Pivot) && 
                   _Rotation.Equals(other._Rotation) &&
                   _LocalScale.Equals(other._LocalScale);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RectTransformSnapshot) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = _AnchoredPosition.GetHashCode();
                hashCode = (hashCode * 397) ^ _SizeDelta.GetHashCode();
                hashCode = (hashCode * 397) ^ _AnchorMin.GetHashCode();
                hashCode = (hashCode * 397) ^ _AnchorMax.GetHashCode();
                hashCode = (hashCode * 397) ^ _Pivot.GetHashCode();
                hashCode = (hashCode * 397) ^ _Rotation.GetHashCode();
                hashCode = (hashCode * 397) ^ _LocalScale.GetHashCode();
                return hashCode;
            }
        }
    }
}
