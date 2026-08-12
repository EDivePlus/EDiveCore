using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EDIVE.GeoToolkit.Area;
using EDIVE.Utils.SerializableDictionary;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace EDIVE.GeoToolkit.MapServices
{
    public abstract class MapServiceDefinition : ScriptableObject
    {
        [FormerlySerializedAs("serverLink")]
        [FormerlySerializedAs("_ServerLink")]
        [PropertySpace]
        [SerializeField]
        [TextArea(1, 5)]
        private string _RequestLink;

        [FormerlySerializedAs("imageFormats")]
        [PropertySpace]
        [SerializeField]
        private List<string> _ImageFormats;

        [FormerlySerializedAs("coordinateSystems")]
        [SerializeField]
        private List<string> _CoordinateSystems;

        [FormerlySerializedAs("layers")]
        [SerializeField]
        private SerializableDictionary<string, string> _Layers;

        [FormerlySerializedAs("sizeLimit")]
        [SerializeField]
        private int2 _SizeLimit;

        [SerializeField]
        [HideInInspector]
        protected bool _IsValid;

        [SerializeField]
        [HideInInspector]
        protected string _StatusMessage;

        public string RequestLink => _RequestLink;
        public IEnumerable<string> ImageFormats => _ImageFormats;
        public IEnumerable<string> CoordinateSystems => _CoordinateSystems;
        public IEnumerable<string> Layers => _Layers != null ? _Layers.Keys : new List<string>();
        public int2 SizeLimit => _SizeLimit;

        /// <summary>
        /// Whether the last update produced a complete, usable definition.
        /// </summary>
        public bool IsValid => _IsValid;

        public abstract string GenerateURL(string coordinateSystem, string imageFormat, string layerTitle, GeoAreaRect bbox, int2 textureSize);

        protected bool TryBeginRequest(string layerTitle, out string layer)
        {
            layer = null;
            if (!_IsValid || string.IsNullOrEmpty(_RequestLink))
            {
                Debug.LogError($"[{name}] Cannot generate URL: the definition is not valid. Run 'Update Data' first.", this);
                return false;
            }

            if (_Layers == null || !_Layers.TryGetValue(layerTitle, out layer))
            {
                Debug.LogError($"[{name}] Cannot generate URL: unknown layer '{layerTitle}'.", this);
                return false;
            }

            return true;
        }

        protected string RequestLinkWithQuery => _RequestLink + (_RequestLink[^1] == '?' ? "" : "?");

        protected void SetCapabilities(string requestLink, List<string> imageFormats, List<string> coordinateSystems, SerializableDictionary<string, string> layers, int2 sizeLimit)
        {
            if (_RequestLink != requestLink)
                ClearData();

            _RequestLink = requestLink;
            _ImageFormats = imageFormats;
            _CoordinateSystems = coordinateSystems;
            _Layers = layers;
            _SizeLimit = sizeLimit;
            _IsValid = true;
            _StatusMessage = $"Valid: {layers.Count} layer(s), {imageFormats.Count} image format(s), {coordinateSystems.Count} coordinate system(s).";
            SetDirty();
        }

        protected void SetInvalid(string message)
        {
            Debug.LogError($"[{name}] Update failed - {message}", this);
            ClearData();
            _IsValid = false;
            _StatusMessage = message;
            SetDirty();
        }

        private void ClearData()
        {
            _RequestLink = null;
            _ImageFormats?.Clear();
            _CoordinateSystems?.Clear();
            _Layers?.Clear();
            _SizeLimit = int2.zero;
        }

        private void SetDirty()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

#if UNITY_EDITOR
        [PropertyOrder(-1)]
        [Button]
        private void UpdateData()
        {
            UniTask.Void(UpdateDataAsync);
        }

        protected abstract UniTaskVoid UpdateDataAsync();
#endif
    }
}
