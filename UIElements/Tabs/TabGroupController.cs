using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
#endif

namespace EDIVE.UIElements.Tabs
{
    public class TabGroupController : MonoBehaviour
    {
        [SerializeField]
        private TabHandler _DefaultTab;

        [SerializeField]
        [ListDrawerSettings(ShowFoldout = false, OnTitleBarGUI = "OnTabsTitleBarGUI")]
        private List<TabHandler> _Tabs;

        private void Start()
        {
            if (_DefaultTab) 
                OnTabSelected(_DefaultTab);
        }

        private void OnEnable()
        {
            _Tabs.ForEach(tab => tab.Selected += OnTabSelected);
        }

        private void OnDisable()
        {
            _Tabs.ForEach(tab => tab.Selected -= OnTabSelected);
        }

        private void OnTabSelected(TabHandler selectedTab)
        {
            _Tabs.ForEach(tab => tab.SetActive(tab == selectedTab));
        }

#if UNITY_EDITOR
        [UsedImplicitly]
        private void OnTabsTitleBarGUI(InspectorProperty property)
        {
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
            {
                _Tabs = GetComponentsInChildren<TabHandler>().ToList();
                property.MarkSerializationRootDirty();
            }
        }
#endif
    }
}
