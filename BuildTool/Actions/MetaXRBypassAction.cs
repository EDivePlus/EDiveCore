using System.Collections;

namespace EDIVE.BuildTool.Actions
{
    // Bypasses errors at OVRGradleGeneration "Your project is using an Oculus XR Plugin version with known issues")
    // by forcing USING_COMPATIBLE_OCULUS_XR_PLUGIN_VERSION
    [System.Serializable]
    public class MetaXRBypassAction : ABuildAction, IStateCaptureBuildCallback
    {
        private const string DEFINE = "USING_COMPATIBLE_OCULUS_XR_PLUGIN_VERSION";

        public override string Tooltip => "Suppresses Meta XR's build guard for non-Oculus Android builds";

        public IEnumerator OnStateCapture(BuildContext context)
        {
            if (context?.Defines != null && !context.Defines.Contains(DEFINE))
                context.Defines.Add(DEFINE);
            yield break;
        }
    }
}
