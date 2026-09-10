using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EDIVE.Rendering.Mirrors
{
    public class PooledMirrorTexture
    {
        public RenderTexture Texture { get; set; }
        public Camera SourceCamera { get; set; }
        public Camera.StereoscopicEye Eye { get; set; }
        
        public bool InUse { get; set; }
    }
    
    public sealed class MirrorResources : IDisposable
    {
        private const string CAMERA_NAME_PREFIX = "[Mirror] Reflection Camera";

        private readonly Dictionary<Camera, Camera> _reflectionCameras = new();
        private readonly HashSet<Camera> _ownedCameras = new();
        private readonly List<PooledMirrorTexture> _textures = new();
        private readonly Queue<RenderTexture> _preallocated = new();
        private readonly List<Camera> _deadCameras = new();

        private readonly string _ownerName;

        public MirrorResources(string ownerName)
        {
            _ownerName = ownerName;
        }

        public int TextureCount => _textures.Count;

        public void Preallocate(MirrorProfile profile, Camera camera, int count)
        {
            ReleasePreallocated();
            for (var i = 0; i < count; i++)
                _preallocated.Enqueue(CreateTexture(profile, camera, true, $"{_ownerName}_prealloc{i}"));
        }

        public Camera GetReflectionCamera(Camera source, MirrorProfile profile)
        {
            if (_reflectionCameras.TryGetValue(source, out var existing) && existing != null)
                return existing;

            var go = new GameObject($"{CAMERA_NAME_PREFIX} for {source.name}", typeof(Camera), typeof(Skybox))
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            var camera = go.GetComponent<Camera>();
            camera.enabled = false;

            var sourceData = source.GetComponent<UniversalAdditionalCameraData>();
            if (sourceData != null)
            {
                var data = go.AddComponent<UniversalAdditionalCameraData>();
                data.renderType = CameraRenderType.Base;
                data.allowXRRendering = sourceData.allowXRRendering;
                Configure(data, profile);
            }

            _reflectionCameras[source] = camera;
            _ownedCameras.Add(camera);
            return camera;
        }

        // True for our own reflection cameras.
        public bool IsReflectionCamera(Camera camera) => _ownedCameras.Contains(camera);

        public static void Configure(UniversalAdditionalCameraData data, MirrorProfile profile)
        {
            if (data == null || profile == null)
                return;

            data.requiresColorOption = profile.OpaqueTexture;
            data.requiresDepthOption = profile.DepthTexture;
            data.renderPostProcessing = profile.RenderPostProcessing;
            data.renderShadows = profile.RenderShadows;
            data.SetRenderer(profile.RendererIndex);
        }

        // Free texture of the right size, or a new one. Held until ReleaseAll.
        public PooledMirrorTexture Acquire(Camera source, Camera.StereoscopicEye eye, MirrorProfile profile)
        {
            var wanted = profile.GetResolution(source);

            foreach (var candidate in _textures)
            {
                if (candidate.InUse || candidate.Eye != eye || candidate.SourceCamera != source)
                    continue;

                // View was resized. Old size is useless, rebuild it.
                if (candidate.Texture == null || candidate.Texture.width != wanted.x || candidate.Texture.height != wanted.y)
                {
                    ReleaseTexture(candidate.Texture);
                    candidate.Texture = CreateTexture(profile, source, source.cameraType != CameraType.SceneView,
                        $"{_ownerName}_{source.name}_{eye}");
                }

                candidate.InUse = true;
                return candidate;
            }

            var allowMsaa = source.cameraType != CameraType.SceneView;
            var preallocated = allowMsaa && _preallocated.Count > 0 ? _preallocated.Dequeue() : null;
            if (preallocated != null && (preallocated.width != wanted.x || preallocated.height != wanted.y))
            {
                ReleaseTexture(preallocated);
                preallocated = null;
            }

            var pooled = new PooledMirrorTexture
            {
                SourceCamera = source,
                Eye = eye,
                InUse = true,
                Texture = preallocated ?? CreateTexture(profile, source, allowMsaa,
                    $"{_ownerName}_{source.name}_{eye}_{_textures.Count}")
            };

            _textures.Add(pooled);
            return pooled;
        }

        public void ReleaseAll()
        {
            foreach (var texture in _textures)
                texture.InUse = false;
        }

        // Drop entries whose camera is gone.
        public void Prune()
        {
            _deadCameras.Clear();
            foreach (var pair in _reflectionCameras)
            {
                if (pair.Key == null || pair.Value == null)
                    _deadCameras.Add(pair.Key);
            }

            foreach (var deadCamera in _deadCameras)
            {
                if (_reflectionCameras.TryGetValue(deadCamera, out var camera) && camera != null)
                {
                    _ownedCameras.Remove(camera);
                    DestroyObject(camera.gameObject);
                }

                _reflectionCameras.Remove(deadCamera);
            }

            for (var i = _textures.Count - 1; i >= 0; i--)
            {
                if (_textures[i].SourceCamera != null && _textures[i].Texture != null)
                    continue;

                ReleaseTexture(_textures[i].Texture);
                _textures.RemoveAt(i);
            }
        }

        // Textures rebuild on the next request.
        public void ReleaseTextures()
        {
            foreach (var pooled in _textures)
            {
                if (pooled.Texture != null && pooled.Texture.IsCreated())
                {
                    foreach (var pair in _reflectionCameras)
                    {
                        if (pair.Value != null && pair.Value.targetTexture == pooled.Texture)
                            pair.Value.targetTexture = null;
                    }
                }

                ReleaseTexture(pooled.Texture);
            }

            _textures.Clear();
            ReleasePreallocated();
        }

        public void Dispose()
        {
            ReleaseTextures();

            foreach (var pair in _reflectionCameras)
            {
                if (pair.Value != null)
                    DestroyObject(pair.Value.gameObject);
            }

            _reflectionCameras.Clear();
            _ownedCameras.Clear();
        }

        private RenderTexture CreateTexture(MirrorProfile profile, Camera camera, bool allowMsaa, string name)
        {
            var texture = new RenderTexture(profile.GetDescriptor(camera, allowMsaa))
            {
                name = $"[Mirror] {name}",
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = profile.WrapMode,
                filterMode = profile.FilterMode
            };

            texture.Create();
            return texture;
        }

        private void ReleasePreallocated()
        {
            while (_preallocated.Count > 0)
                ReleaseTexture(_preallocated.Dequeue());
        }

        private static void ReleaseTexture(RenderTexture texture)
        {
            if (texture == null)
                return;

            texture.Release();
            DestroyObject(texture);
        }

        private static void DestroyObject(UnityEngine.Object target)
        {
            if (target == null)
                return;

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(target);
            else
                UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
