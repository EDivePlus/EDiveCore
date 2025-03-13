using System.Diagnostics;
using Sirenix.OdinInspector;

namespace EDIVE.OdinExtensions.Attributes
{
    [Conditional("UNITY_EDITOR")]
    public class OnSceneGUIAttribute : ShowInInspectorAttribute
    {
        
    }
}
