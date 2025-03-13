using System.Text;
using UnityEngine;

namespace EDIVE.NativeUtils
{
    public static class LayerExtensions
    {
        public static bool ContainsLayer(this LayerMask layerMask, int layer)
        {
            return layerMask == (layerMask | (1 << layer));
        }
        
        public static bool IsInLayerMask(this GameObject gameObject, LayerMask layerMask)
        {
            return layerMask.ContainsLayer(gameObject.layer);
        }
        
        public static bool IsInLayerMask(this Component component, LayerMask layerMask)
        {
            return IsInLayerMask(component.gameObject, layerMask);
        }
        
        public static string GetCommaSeparatedLayers(this LayerMask layerMask)
        {
            var stringBuilder = new StringBuilder();
            for (var i = 0; i < 32; i++)
            {
                if (layerMask == (layerMask | (1 << i)))
                {
                    if (stringBuilder.Length != 0) stringBuilder.Append(",");

                    stringBuilder.Append(LayerMask.LayerToName(i));
                }
            }
            return stringBuilder.ToString();
        }
    }
}