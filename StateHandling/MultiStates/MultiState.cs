using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.StateHandling.MultiStates
{
    public class MultiState : AMultiState
    {
        [SerializeField]
        [InlineProperty]
        [HideReferenceObjectPicker]
        [ListDrawerSettings(ShowFoldout = false)]
        internal List<MultiStateRecord> _StatePresets = new();

        protected override bool TrySetStateInternal(string stateID, bool immediate = false)
        {
            if (_StatePresets == null)
                return false;

            if (!_StatePresets.TryGetFirst(s => s.StateID == stateID, out var statePreset))
            {
                Debug.LogError($"[MultiState] No state with ID '{stateID}'", this);
                return false;
            }

            statePreset.Apply();
            return true;
        }

        public override IEnumerable<string> GetAllStates()
        {
            return _StatePresets.Select(statePreset => statePreset.StateID);
        }

#if UNITY_EDITOR
        public override void AddState(string id)
        {
            _StatePresets.Add(new MultiStateRecord(id));
        }

        public override bool RemoveState(string id)
        {
            return _StatePresets.RemoveAll(s => s.StateID == id) > 0;
        }
#endif
    }
    
    [Serializable]
    public class MultiStateRecord
    {
        [EnhancedFoldoutGroup("State", nameof(GetFoldoutColor), true)]
        [ShowInFoldoutHeader]
        [HideLabel]
        [SerializeField]
        private string _StateID;

        [EnhancedFoldoutGroup("State")]
        [HideLabel]
        [SerializeField]
        [InlineProperty]
        private ObjectStatePresetField _ObjectPresets = new();

        public string StateID
        {
            get => _StateID;
            set => _StateID = value;
        }

        public IReadOnlyList<ObjectStatePresetRecord> ObjectPresets => _ObjectPresets.ObjectPresets;

        public MultiStateRecord() { }
        public MultiStateRecord(string stateID)
        {
            _StateID = stateID;
        }
        
        public MultiStateRecord(string stateID, List<ObjectStatePresetRecord> objectPresets)
        {
            _StateID = stateID;
            _ObjectPresets = new ObjectStatePresetField(objectPresets);
        }
        
        public void Apply()
        {
            _ObjectPresets.Apply();
        }

#if UNITY_EDITOR
        [EnhancedFoldoutGroup("State")]
        [ShowInFoldoutHeader]
        [OnInspectorGUI]
        private void DrawControls(InspectorProperty property)
        {
            GUILayout.Space(4);
            var rect = GUILayoutUtility.GetRect(18, 18, SirenixGUIStyles.Button, GUILayoutOptions.ExpandWidth(false).Width(18));
            if (SirenixEditorGUI.IconButton(rect, FontAwesomeEditorIcons.UploadSolid, "Apply"))
            {
                foreach (var record in _ObjectPresets.ObjectPresets)
                {
                    if (record.Target == null)
                        continue;
                    Undo.RecordObject(record.Target, "Apply state presets");
                    record.Apply();
                    record.SetDirty();
                }
            }
            
            rect = GUILayoutUtility.GetRect(18, 18, SirenixGUIStyles.Button, GUILayoutOptions.ExpandWidth(false).Width(18));
            if (SirenixEditorGUI.IconButton(rect, FontAwesomeEditorIcons.DownloadSolid, "Capture"))
            {
                foreach (var record in _ObjectPresets.ObjectPresets)
                {
                    if (record.Target == null)
                        continue;
                    record.Capture();
                }
                property.MarkSerializationRootDirty();
            }
        }
        
        private Color GetFoldoutColor(InspectorProperty property)
        {
            var element = property.FindParent(p => p.Parent?.ChildResolver is IOrderedCollectionResolver, true);
            return element != null ? ColorTools.GetRainbowColor(element.Index, element.Parent.Children.Count) : Color.gray;
        }
#endif
    }
}
