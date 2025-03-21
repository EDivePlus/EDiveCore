using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEditor;

namespace EDIVE.BuildTool.Actions
{
    [System.Serializable]
    public abstract class ABuildAction : System.IComparable<ABuildAction>
    {
        public virtual UniTask OnPreBuildBeforeDefines(BuildContext buildContext) => UniTask.CompletedTask;
        public virtual UniTask OnPreBuildAfterDefines(BuildContext buildContext) => UniTask.CompletedTask;

        public virtual UniTask OnPostBuildBeforeDefines(BuildContext buildContext) => UniTask.CompletedTask;
        public virtual UniTask OnPostBuildAfterDefines(BuildContext buildContext) => UniTask.CompletedTask;

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
