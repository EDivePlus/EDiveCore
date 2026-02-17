using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
#endif

namespace EDIVE.Replay.Components
{
    [Serializable]
    public abstract class AReplayAgentComponent
    {
        public abstract string ID { get; }
        public abstract float MinTime { get; }
        public abstract float MaxTime { get; }
        
        public abstract string ComponentLabel { get; }
        protected abstract string TargetID { get; }
        protected abstract GameObject TargetGameObject { get; }
        public abstract Type EditorTargetType { get; }
        
        public abstract AReplayAgentComponentData GetDataCopy();
        public abstract void SetData(AReplayAgentComponentData data);

        public abstract void StartRecording(float startTime, ReplayRecordingConfig config, CancellationToken cancellationToken = default);
        public virtual UniTask PreparePlayback(float startTime, CancellationToken cancellationToken = default) => UniTask.CompletedTask;
        public abstract void StartPlayback(float startTime, CancellationToken cancellationToken = default);
        public abstract void ApplyTime(float time);
        public abstract void ClearData(float startTime = 0f);
        
#if UNITY_EDITOR
        public string GenerateID(InspectorProperty property)
        {
            var resultID = $"{TargetGameObject.name}{TargetID}";
            if (!property.TryGetParentObject<IEnumerable<AReplayAgentComponent>>(out var parentList))
                return resultID;

            var existingIds = parentList
                .Where(c => !ReferenceEquals(c, this))
                .Select(c => c.ID)
                .Where(id => id != null)
                .ToArray();
            
            return ObjectNames.GetUniqueName(existingIds, resultID);
        }
        
        [PropertyOrder(-100)]
        [OnInspectorGUI]
        public void DrawTitle()
        {
            var icon = GUIHelper.GetAssetThumbnail(null, EditorTargetType, false);
            var content = GUIHelper.TempContent($" {ComponentLabel}", icon);
            GUILayout.Label(content, SirenixGUIStyles.BoldLabel, GUILayout.Height(18));
        }
#endif
    }

    [Serializable]
    public abstract class AReplayAgentComponent<TData> : AReplayAgentComponent
        where TData : AReplayAgentComponentData, new()
    {
        [PropertyOrder(10)]
        [HideLabel]
        [InlineProperty]
        [SerializeField]
        protected TData _Data = new();
        
        public override string ID => _Data.ID;
        
        public override float MinTime => _Data?.GetMinTime() ?? 0f;
        public override float MaxTime => _Data?.GetMaxTime() ?? 0f;

        protected AReplayAgentComponent() { }
        protected AReplayAgentComponent( TData data)
        {
            _Data = data;
        }

        public override AReplayAgentComponentData GetDataCopy() => _Data.GetCopy();

        public override void SetData(AReplayAgentComponentData data)
        {
            if (data is not TData typedData)
            {
                Debug.LogError($"{GetType().Name} does not support {nameof(TData)}");
                return;
            }
            _Data = typedData;
        }
    }
    
    [Serializable]
    public abstract class AReplayAgentComponent<TTarget, TData> : AReplayAgentComponent<TData> 
        where TTarget : UnityEngine.Object
        where TData : AReplayAgentComponentData, new()
    {
        [PropertyOrder(-1)]
        [SerializeField]
        protected TTarget _Target;

        protected override GameObject TargetGameObject => _Target.TryGetGameObject(out var go) ? go : null;
        public override Type EditorTargetType => typeof(TTarget);

        protected AReplayAgentComponent() { }
        protected AReplayAgentComponent(TTarget target, TData data) : base(data) { }
    }
}
