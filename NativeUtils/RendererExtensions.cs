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

        private static readonly List<Material> MATERIALS_BUFFER = new();

        // No allocation, unlike sharedMaterials.
        public static bool TryGetSharedMaterial(this Renderer renderer, int index, out Material material)
        {
            material = null;
            if (renderer == null || index < 0)
                return false;

            renderer.GetSharedMaterials(MATERIALS_BUFFER);
            var found = index < MATERIALS_BUFFER.Count;
            if (found)
                material = MATERIALS_BUFFER[index];

            // Do not keep materials alive.
            MATERIALS_BUFFER.Clear();
            return found;
        }

        // Copies the array, so avoid it every frame.
        public static bool SetSharedMaterial(this Renderer renderer, int index, Material material)
        {
            if (renderer == null || index < 0)
                return false;

            var materials = renderer.sharedMaterials;
            if (index >= materials.Length)
                return false;

            if (materials[index] == material)
                return true;

            materials[index] = material;
            renderer.sharedMaterials = materials;
            return true;
        }

        // Index -1 = whole renderer.
        public static void GetPropertyBlockAt(this Renderer renderer, MaterialPropertyBlock block, int index)
        {
            if (index < 0)
                renderer.GetPropertyBlock(block);
            else
                renderer.GetPropertyBlock(block, index);
        }

        // Index -1 = whole renderer.
        public static void SetPropertyBlockAt(this Renderer renderer, MaterialPropertyBlock block, int index)
        {
            if (index < 0)
                renderer.SetPropertyBlock(block);
            else
                renderer.SetPropertyBlock(block, index);
        }

        // Index -1 = whole renderer.
        public static void ClearPropertyBlock(this Renderer renderer, int index = -1)
        {
            if (renderer != null)
                renderer.SetPropertyBlockAt(null, index);
        }
    }
}
