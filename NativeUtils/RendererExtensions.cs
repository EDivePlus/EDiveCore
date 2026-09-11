using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace EDIVE.NativeUtils
{
    public static class RendererExtensions
    {
        // Material index -1 = all materials. Hidden properties skipped.
        public static List<string> GetShaderPropertyNames(this Renderer renderer, int materialIndex, params ShaderPropertyType[] propertyTypes)
        {
            var names = new List<string>();
            if (renderer == null)
                return names;

            var seen = new HashSet<string>();
            var materials = renderer.sharedMaterials;
            for (var m = 0; m < materials.Length; m++)
            {
                if (materialIndex >= 0 && m != materialIndex)
                    continue;

                var material = materials[m];
                if (material == null || material.shader == null)
                    continue;

                var shader = material.shader;
                var count = shader.GetPropertyCount();
                for (var i = 0; i < count; i++)
                {
                    if ((shader.GetPropertyFlags(i) & ShaderPropertyFlags.HideInInspector) != 0)
                        continue;
                    if (Array.IndexOf(propertyTypes, shader.GetPropertyType(i)) < 0)
                        continue;

                    var name = shader.GetPropertyName(i);
                    if (seen.Add(name))
                        names.Add(name);
                }
            }
            return names;
        }
    }
}
