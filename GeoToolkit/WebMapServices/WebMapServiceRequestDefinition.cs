using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.GeoToolkit.WebMapServices
{
    public class WebMapServiceRequestDefinition : ScriptableObject
    {
        [InlineProperty, HideLabel]
        [SerializeField]
        private WebMapServiceRequest _Request;
        public WebMapServiceRequest Request => _Request;
    }
}