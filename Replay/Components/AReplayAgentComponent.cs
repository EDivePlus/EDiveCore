using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        
        public abstract UnityEngine.Object BaseTarget { get; }
        public abstract Type TargetType { get; }
        public abstract string ComponentLabel { get; }
        protected abstract string TargetID { get; }
        
        public abstract AReplayAgentComponentData GetDataCopy();
        public abstract void SetData(AReplayAgentComponentData data);

        public abstract void StartRecording(float startTime, ReplayRecordingConfig config, CancellationToken cancellationToken = default);
        public abstract void StartPlayback(float startTime, CancellationToken cancellationToken = default);
        public abstract void ApplyTime(float time);
        public abstract void ClearData(float startTime = 0f);
        
#if UNITY_EDITOR
        public string GenerateID(InspectorProperty property)
        {
            if (!BaseTarget.TryGetComponent<Transform>(out var targetTr))
                return TargetID;

            var resultID = $"{targetTr.gameObject.name}{TargetID}";
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
        public void DrawTitle() => GUILayout.Label(ComponentLabel, SirenixGUIStyles.BoldLabel);
#endif
    }
    
    [Serializable]
    public abstract class AReplayAgentComponent<TTarget, TData> : AReplayAgentComponent 
        where TTarget : UnityEngine.Object
        where TData : AReplayAgentComponentData, new()
    {
        [SerializeField]
        protected TTarget _Target;
        
        [HideLabel]
        [InlineProperty]
        [SerializeField]
        protected TData _Data = new();

        public override UnityEngine.Object BaseTarget => _Target;
        public override Type TargetType => typeof(TTarget);
        public override string ID => _Data.ID;
        
        public override float MinTime => _Data?.GetMinTime() ?? 0f;
        public override float MaxTime => _Data?.GetMaxTime() ?? 0f;

        protected AReplayAgentComponent() { }
        protected AReplayAgentComponent(TTarget target, TData data)
        {
            _Target = target;
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
}
