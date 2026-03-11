// Author: Michal Petr
// Created: 29.10.2025

using System;
using EDIVE.DataStructures;
using EDIVE.DataStructures.RectTransformSnapshot;
using EDIVE.NativeUtils;
using EDIVE.VisualPresets.Presets;
using EDIVE.VisualPresets.VisualIDs;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;
using Object = UnityEngine.Object;

namespace EDIVE.VisualPresets.Switchers
{
    [Serializable]
    public class PrefabVisualSwitcherRecord : AVisualSwitcherRecord<PrefabVisualID>
    {
        [VerticalGroup("Value")]
        [SerializeField]
        private Transform _Root;

        public override string EditorLabel => "Prefab";
        public override Type EditorIconTargetType => typeof(GameObject);
        
        public Transform Root => _Root;
    }
    
    [Preserve]
    public class PrefabVisualSwitcherStrategy : AVisualSwitcherStrategy<PrefabVisualID, PrefabVisualPresetRecord, PrefabVisualSwitcherRecord>
    {
        protected override IDisposable Apply(PrefabVisualPresetRecord presetRecord, PrefabVisualSwitcherRecord switcherRecord)
        {
            if (switcherRecord.Root == null)
                return null;
            
            switcherRecord.Root.DestroyChildren();

            // Instantiate the new prefab
            GameObject newObj = null;
#if UNITY_EDITOR
            if (!Application.isPlaying) 
                newObj = UnityEditor.PrefabUtility.InstantiatePrefab(presetRecord.Prefab, switcherRecord.Root) as GameObject;
#endif
            
            if (newObj == null) 
                newObj = Object.Instantiate(presetRecord.Prefab, switcherRecord.Root);
            
            if (newObj != null)
            {
                newObj.name = presetRecord.Prefab.name;
                newObj.hideFlags = HideFlags.DontSave;
                if (presetRecord.Prefab.transform is RectTransform srcRectTr)
                {
                    var rectSnapshot = new RectTransformSnapshot(srcRectTr);
                    rectSnapshot.ApplyTo((RectTransform) newObj.transform);
                }
                else
                {
                    var snapshot = new TransformSnapshot(presetRecord.Prefab.transform);
                    snapshot.ApplyTo(newObj.transform);
                }
            }

            return DisposableUtils.Create(() =>
            {
                if (switcherRecord.Root != null)
                {
                    switcherRecord.Root.DestroyChildren();
                }
            });
        }
    }
}
