using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
#endif

namespace EDIVE.Tweening
{
    public abstract class ATweenPreset : ScriptableObject, ITweenPreset
    {
#if UNITY_EDITOR
        [ShowInInspector]
        [ListDrawerSettings(IsReadOnly = true, OnTitleBarGUI = nameof(OnReferencesTitleBarGUI))]
        private List<TweenObjectReference> _references;

        private void OnReferencesTitleBarGUI()
        {
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
            {
                RefreshReferences();
            }
        }

        [OnInspectorInit]
        protected void RefreshReferences()
        {
            try
            {
                var references = new HashSet<TweenObjectReference>();
                PopulateReferences(references);
                _references = references.Where(t => t != null).ToList();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public abstract void PopulateReferences(HashSet<TweenObjectReference> references);
        public abstract void PopulateTargets(TweenTargetCollection targets);
#endif
    }
}
