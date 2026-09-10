using System;
using System.Collections.Generic;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

namespace EDIVE.Rendering.Mirrors
{
    [Flags]
    public enum MirrorFlipApi
    {
        [LabelText("Direct3D")] Direct3D = 1 << 0,
        Vulkan = 1 << 1,
        Metal = 1 << 2,
        [LabelText("OpenGL")] OpenGL = 1 << 3,
        Other = 1 << 4,
        Any = Direct3D | Vulkan | Metal | OpenGL | Other
    }

    [Flags]
    public enum MirrorFlipContext
    {
        SceneView = 1 << 0,
        GameView = 1 << 1,
        Player = 1 << 2,
        Any = SceneView | GameView | Player
    }

    [Flags]
    public enum MirrorFlipDepth
    {
        First = 1 << 0,
        SecondAndDeeper = 1 << 1,
        Any = First | SecondAndDeeper
    }

    [Serializable]
    public class MirrorFlipRule
    {
        [EnhancedTableColumn("API")]
        [SerializeField]
        private MirrorFlipApi _Api = MirrorFlipApi.Any;

        [SerializeField]
        private MirrorFlipContext _Context = MirrorFlipContext.Any;

        [SerializeField]
        private MirrorFlipDepth _Depth = MirrorFlipDepth.Any;

        [SerializeField]
        private bool _Flip = true;

        public bool Flip => _Flip;

        public bool Matches(MirrorFlipApi api, MirrorFlipContext context, MirrorFlipDepth depth)
        {
            return Allows((int) _Api, (int) api)
                   && Allows((int) _Context, (int) context)
                   && Allows((int) _Depth, (int) depth);
        }
        
        private static bool Allows(int mask, int value) => mask == 0 || (mask & value) != 0;
    }
    
    public class MirrorFlipRules : ScriptableObject
    {
        [SerializeField]
        [InfoBox("Some graphics APIs store the reflection upside down. Add a rule only if you see a flipped mirror. First match wins.")]
        [EnhancedTableList]
        private List<MirrorFlipRule> _Rules = new();

        public IReadOnlyList<MirrorFlipRule> Rules => _Rules;

        public bool Evaluate(Camera camera, int depth)
        {
            if (_Rules == null || _Rules.Count == 0)
                return false;

            var api = GetApi();
            var context = GetContext(camera);
            var depthFlag = depth <= 1 ? MirrorFlipDepth.First : MirrorFlipDepth.SecondAndDeeper;

            foreach (var rule in _Rules)
            {
                if (rule != null && rule.Matches(api, context, depthFlag))
                    return rule.Flip;
            }

            return false;
        }

        private static MirrorFlipApi GetApi()
        {
            return SystemInfo.graphicsDeviceType switch
            {
                GraphicsDeviceType.Direct3D11 or GraphicsDeviceType.Direct3D12 => MirrorFlipApi.Direct3D,
                GraphicsDeviceType.Vulkan => MirrorFlipApi.Vulkan,
                GraphicsDeviceType.Metal => MirrorFlipApi.Metal,
                GraphicsDeviceType.OpenGLCore or GraphicsDeviceType.OpenGLES3 => MirrorFlipApi.OpenGL,
                _ => MirrorFlipApi.Other
            };
        }

        private static MirrorFlipContext GetContext(Camera camera)
        {
            if (!Application.isEditor)
                return MirrorFlipContext.Player;

            return camera != null && camera.cameraType == CameraType.SceneView
                ? MirrorFlipContext.SceneView
                : MirrorFlipContext.GameView;
        }
    }
}
