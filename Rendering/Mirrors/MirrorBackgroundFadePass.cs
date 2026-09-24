using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace EDIVE.Rendering.Mirrors
{
    // Fades the reflection alpha out before the far clip, so geometry dissolves into the environment instead of popping.
    public class MirrorBackgroundFadePass : ScriptableRenderPass
    {
        private static readonly int FADE_LENGTH = Shader.PropertyToID("_MirrorFadeLength");
        private static readonly int CULL_PLANE = Shader.PropertyToID("_MirrorCullPlane");

        private readonly Material _material;
        private Vector4 _cullPlane;

        private class PassData
        {
            public Material Material;
            public TextureHandle Depth;
            public Vector4 CullPlane;
        }

        public MirrorBackgroundFadePass(Material material)
        {
            _material = material;
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public void SetFadeLength(float length)
        {
            _material.SetFloat(FADE_LENGTH, Mathf.Max(length, 0.01f));
        }

        // Far plane of the culling frustum. Visibility volumes can pull it in closer than the camera far clip.
        public void SetCullingMatrix(Matrix4x4 cullingMatrix)
        {
            var plane = cullingMatrix.GetRow(3) - cullingMatrix.GetRow(2);
            _cullPlane = plane / ((Vector3) plane).magnitude;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resources = frameData.Get<UniversalResourceData>();
            if (!resources.cameraDepthTexture.IsValid())
                return;

            using var builder = renderGraph.AddRasterRenderPass<PassData>("Mirror Background Fade", out var data);
            data.Material = _material;
            data.Depth = resources.cameraDepthTexture;
            data.CullPlane = _cullPlane;

            builder.UseTexture(data.Depth);
            builder.SetRenderAttachment(resources.activeColorTexture, 0);
            // Global, so each reflection keeps its own plane until the GPU runs it.
            builder.AllowGlobalStateModification(true);
            builder.SetRenderFunc(static (PassData pass, RasterGraphContext context) =>
            {
                context.cmd.SetGlobalVector(CULL_PLANE, pass.CullPlane);
                Blitter.BlitTexture(context.cmd, pass.Depth, new Vector4(1f, 1f, 0f, 0f), pass.Material, 0);
            });
        }
    }
}
