using System;
using System.Diagnostics;

namespace EDIVE.OdinExtensions.Attributes
{
    [Conditional("UNITY_EDITOR")]
    public class KeepRefreshingAttribute : Attribute
    {
        public bool UpdateInEditorMode;
        public bool UpdateInPlayMode;

        public KeepRefreshingAttribute(bool updateInEditorMode = true, bool updateInPlayMode = true)
        {
            UpdateInEditorMode = updateInEditorMode;
            UpdateInPlayMode = updateInPlayMode;
        }
    }
}
