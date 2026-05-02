using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.AppLoading.LoadItems;
using EDIVE.DataStructures.TypeStructures;
using EDIVE.OdinExtensions;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
#endif


namespace EDIVE.AppLoading.Dependencies
{
    [Serializable]
    public class TypedDependency
    {
        [HideInInspector]
        [SerializeField]
        private UType _Type;

        public Type Type => _Type;

        public TypedDependency() { }
        public TypedDependency(Type type)
        {
            _Type = new UType(type);
        }

        public IEnumerable<LoadItemDefinition> Resolve(DependencyResolutionContext context)
        {
            var resolvedType = _Type?.Value;
            if (resolvedType == null) yield break;
            foreach (var item in context.GetItemsRepresentingType(resolvedType))
                yield return item;
        }

#if UNITY_EDITOR
        [HorizontalGroup]
        [OnInspectorGUI]
        private void DrawLabel()
        {
            SirenixEditorFields.TextField(Type != null ? $"{Type.Name}" : "NULL");
            if (Type != null)
                GUI.Label(GUILayoutUtility.GetLastRect(), new GUIContent(string.Empty, Type.FullName ?? Type.AssemblyQualifiedName));
        }
        
        [HorizontalGroup]
        [ShowInInspector]
        [PropertyOrder(10)]
        [ListDrawerSettings(IsReadOnly = true, OnTitleBarGUI = "OnCandidatesTitleBarGUI")]
        public List<LoadItemDefinition> Candidates { get; private set; } = new();
        
        private void OnCandidatesTitleBarGUI(InspectorProperty property)
        {
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
                ResolveTypedDependencyCandidates(property);
        }
        
        [OnInspectorInit]
        private void ResolveTypedDependencyCandidates(InspectorProperty property)
        {
            var parent = property.TryGetParentObject<LoadItemDefinition>(out var self) ? self : null;
            ResolveTypedDependencyCandidates(parent);
        }
        
        public void ResolveTypedDependencyCandidates(LoadItemDefinition parent)
        {
            var ctx = DependencyResolutionContext.EditorContext;
            Candidates = ctx.GetItemsRepresentingType(Type)
                .Where(c => c != null && c != parent)
                .ToList();
        }
#endif
    }
}
