#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EDIVE.Rendering.Mirrors
{
    // Colour and distance cubes for reprojecting a mirror. Kept apart so the march reads only the small one.
    public static class MirrorDepthProbeBaker
    {
        private const string SHADER_NAME = "Hidden/EDIVE/MirrorDepthProbeBake";
        private const int DISTANCE_PASS = 0;

        // Where nothing was hit. Still fits in half.
        private const float EMPTY_DISTANCE = 60000f;

        private static readonly int BAKE_DEPTH = Shader.PropertyToID("_BakeDepth");
        private static readonly int BAKE_Z_PARAMS = Shader.PropertyToID("_BakeZParams");
        private static readonly int BAKE_FAR = Shader.PropertyToID("_BakeFar");
        private static readonly int BAKE_EMPTY = Shader.PropertyToID("_BakeEmpty");

        // Unity face order.
        private static readonly Vector3[] FACE_FORWARD =
        {
            Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back
        };

        private static readonly Vector3[] FACE_UP =
        {
            Vector3.up, Vector3.up, Vector3.back, Vector3.forward, Vector3.up, Vector3.up
        };

        public static bool Bake(Vector3 origin, int colorResolution, int distanceResolution, float near, float far,
            int cullingMask, string assetPathPrefix, out Cubemap colorCube, out Cubemap distanceCube)
        {
            colorCube = null;
            distanceCube = null;

            var shader = Shader.Find(SHADER_NAME);
            if (shader == null)
            {
                Debug.LogError($"[Mirrors] Missing shader {SHADER_NAME}.");
                return false;
            }

            var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            var cameraObject = new GameObject("MirrorDepthProbeCamera") { hideFlags = HideFlags.HideAndDontSave };
            var cam = cameraObject.AddComponent<Camera>();
            cam.enabled = false;
            cam.fieldOfView = 90f;
            cam.aspect = 1f;
            cam.nearClipPlane = near;
            cam.farClipPlane = far;
            cam.cullingMask = cullingMask;
            cam.allowHDR = true;
            cam.allowMSAA = false;
            cam.clearFlags = CameraClearFlags.Skybox;

            var cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = false;
            cameraData.antialiasing = AntialiasingMode.None;
            cameraData.renderShadows = true;

            // URP only hands over depth through a request whose target is depth only.
            var faceColor = new RenderTexture(colorResolution, colorResolution, GraphicsFormat.R16G16B16A16_SFloat, GraphicsFormat.D32_SFloat);
            var faceDepth = new RenderTexture(distanceResolution, distanceResolution, GraphicsFormat.None, GraphicsFormat.D32_SFloat);
            var faceDistance = new RenderTexture(distanceResolution, distanceResolution, 0, GraphicsFormat.R32_SFloat);
            var colorReadback = new Texture2D(colorResolution, colorResolution, TextureFormat.RGBAHalf, false, true);
            var distanceReadback = new Texture2D(distanceResolution, distanceResolution, TextureFormat.RFloat, false, true);

            // Mips stop the colour shimmering. Distance skips them, blending it across edges invents surfaces.
            var color = new Cubemap(colorResolution, TextureFormat.RGB9e5Float, true);
            var distance = new Cubemap(distanceResolution, TextureFormat.RHalf, false);

            var previous = RenderTexture.active;
            try
            {
                material.SetVector(BAKE_Z_PARAMS, GetZBufferParams(near, far));
                material.SetFloat(BAKE_FAR, far);
                material.SetFloat(BAKE_EMPTY, EMPTY_DISTANCE);

                for (var face = 0; face < 6; face++)
                {
                    cam.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(FACE_FORWARD[face], FACE_UP[face]));
                    if (!Render(cam, faceColor) || !Render(cam, faceDepth))
                    {
                        Object.DestroyImmediate(color);
                        Object.DestroyImmediate(distance);
                        return false;
                    }

                    material.SetTexture(BAKE_DEPTH, faceDepth, RenderTextureSubElement.Depth);
                    Graphics.Blit(null, faceDistance, material, DISTANCE_PASS);

                    color.SetPixels(ReadFace(faceColor, colorReadback), (CubemapFace) face);
                    distance.SetPixels(ReadFace(faceDistance, distanceReadback), (CubemapFace) face);
                }

                color.Apply(true);
                distance.Apply(false);
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(colorReadback);
                Object.DestroyImmediate(distanceReadback);
                faceColor.Release();
                faceDepth.Release();
                faceDistance.Release();
                Object.DestroyImmediate(faceColor);
                Object.DestroyImmediate(faceDepth);
                Object.DestroyImmediate(faceDistance);
            }

            color.filterMode = FilterMode.Trilinear;
            color.wrapMode = TextureWrapMode.Clamp;
            distance.filterMode = FilterMode.Bilinear;
            distance.wrapMode = TextureWrapMode.Clamp;

            colorCube = SaveAsset(color, assetPathPrefix + "-Color.asset");
            distanceCube = SaveAsset(distance, assetPathPrefix + "-Distance.asset");
            return true;
        }

        // Rendered bottom up, cube faces are top down.
        private static Color[] ReadFace(RenderTexture source, Texture2D readback)
        {
            RenderTexture.active = source;
            readback.ReadPixels(new Rect(0, 0, readback.width, readback.height), 0, 0, false);
            return FlipRows(readback.GetPixels(), readback.width);
        }

        private static bool Render(Camera cam, RenderTexture destination)
        {
            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = destination };
            if (!RenderPipeline.SupportsRenderRequest(cam, request))
            {
                Debug.LogError("[Mirrors] URP refused the depth probe render request.");
                return false;
            }

            RenderPipeline.SubmitRenderRequest(cam, request);
            return true;
        }

        private static Vector4 GetZBufferParams(float near, float far)
        {
            if (SystemInfo.usesReversedZBuffer)
            {
                var x = -1f + far / near;
                return new Vector4(x, 1f, x / far, 1f / far);
            }

            var nx = 1f - far / near;
            var ny = far / near;
            return new Vector4(nx, ny, nx / far, ny / far);
        }

        private static Color[] FlipRows(Color[] pixels, int size)
        {
            var flipped = new Color[pixels.Length];
            for (var y = 0; y < size; y++)
                System.Array.Copy(pixels, y * size, flipped, (size - 1 - y) * size, size);
            return flipped;
        }

        // Overwrites in place so references survive a rebake.
        private static Cubemap SaveAsset(Cubemap cube, string assetPath)
        {
            cube.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            var existing = AssetDatabase.LoadAssetAtPath<Cubemap>(assetPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(cube, assetPath);
                existing = cube;
            }
            else
            {
                EditorUtility.CopySerialized(cube, existing);
                Object.DestroyImmediate(cube);
            }

            // GPU only in builds.
            var serialized = new SerializedObject(existing);
            serialized.FindProperty("m_IsReadable").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssetIfDirty(existing);
            return existing;
        }
    }
}
#endif
