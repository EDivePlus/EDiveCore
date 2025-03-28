using System.Collections;
using Sirenix.OdinInspector;
using UnityEditor;

namespace EDIVE.BuildTool.Actions
{
    [System.Serializable]
    public abstract class ABuildAction : System.IComparable<ABuildAction>
    {
        public virtual IEnumerator OnPreBuildBeforeDefines(BuildContext buildContext) { yield break; }
        public virtual IEnumerator OnPreBuildAfterDefines(BuildContext buildContext) { yield break; }

        public virtual IEnumerator OnPostBuildBeforeDefines(BuildContext buildContext) { yield break; }
        public virtual IEnumerator OnPostBuildAfterDefines(BuildContext buildContext) { yield break; }

        public virtual int Priority => 0;

        [OnInspectorGUI]
        [PropertyOrder(-100)]
        private void DrawLabel()
        {
            EditorGUILayout.LabelField($"{Label}", EditorStyles.boldLabel);
        }

        public virtual string Label => ObjectNames.NicifyVariableName(GetType().Name);

        public int CompareTo(ABuildAction other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return Priority.CompareTo(other.Priority);
        }
    }
}
