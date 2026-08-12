using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.GeoToolkit.MapServices
{
    public class MapServiceRequestDefinition : ScriptableObject
    {
        [InlineProperty, HideLabel]
        [SerializeField]
        private MapServiceRequest _Request;
        public MapServiceRequest Request => _Request;
    }
}