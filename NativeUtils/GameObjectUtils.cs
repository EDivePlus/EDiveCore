// Author: František Holubec
// Created: 05.03.2026

using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EDIVE.NativeUtils
{
    public static class GameObjectUtils
    {
        public static T Instantiate<T>(T prefab, Transform parent) where T : Object
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return (T) PrefabUtility.InstantiatePrefab(prefab, parent);
#endif
            return Object.Instantiate(prefab, parent);
        }

        // Runtime quad without collider, shadows and probes
        public static Renderer CreateQuad(string name, Transform parent, Material material)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            Object.Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(parent, true);

            var renderer = quad.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return renderer;
        }
    }
}
