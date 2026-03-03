// Author: Michal Petr
// Created: 03.03.2026

using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using EDIVE.NativeUtils;
using UnityEngine;

namespace EDIVE.Tablet
{
    public class TabletController : MonoBehaviour
    {
        [SerializeField]
        private TabletDefinition _Definition;
        
        [SerializeField]
        private Transform _FrameRoot;
        
        [SerializeField]
        private TabletFrame _FramePrefab;
        
        [SerializeField]
        private Transform _PinnedDisplaysRoot;
        
        private TabletFrame CurrentFrame { get; set; }
        private List<TabletFrame> ActiveFrames { get; set; } = new();
        private List<TabletWidgetDefinitionDisplay> PinnedDisplays { get; set; } = new();

        private void Awake()
        {
            SetupPinnedDisplays();
            _FrameRoot.GetComponentsInChildren<TabletFrame>().ForEach(persistentFrame =>
            {
                if (persistentFrame.IsPersistent)
                {
                    ActiveFrames.Add(persistentFrame);
                    persistentFrame.Initialize(this).Forget();
                    persistentFrame.gameObject.SetActive(false);
                }
                else
                    Destroy(persistentFrame.gameObject);
            });
        }

        public void OpenWidget(TabletWidgetDefinition definition)
        {
            OpenView(new ReferenceTabletViewSource(definition.WidgetView));
        }
        
        private void OpenView(ITabletViewSource view)
        {
            if (TryOpenActiveFrame(view))
                return;

            CreateNewFrame(view);
        }
        
        private bool TryGetActiveFrame(ITabletViewSource view, out TabletFrame frame)
        {
            frame = ActiveFrames.Find(f => Equals(f.ViewSource, view));
            return frame != null;
        }
        
        private bool TryOpenActiveFrame(ITabletViewSource view)
        {
            if (!TryGetActiveFrame(view, out var frame)) 
                return false;
            OpenFrame(frame);
            return true;
        }
        
        private void CreateNewFrame(ITabletViewSource view, bool open = true)
        {
            var newFrame = Instantiate(_FramePrefab, _FrameRoot);
            newFrame.Initialize(this, view).Forget();
            ActiveFrames.Add(newFrame);
            if (open) OpenFrame(newFrame);
        }
        
        public void OpenFrame(TabletFrame frame)
        {
            CollapseCurrentFrame();
            CurrentFrame = frame;
            CurrentFrame.gameObject.SetActive(true);
            frame.Open();
        }
        
        public void CollapseCurrentFrame()
        {
            if (CurrentFrame)
            {
                CurrentFrame.Collapse();
                CurrentFrame.gameObject.SetActive(false);
            }
            CurrentFrame = null;
        }
        
        public void TerminateFrame(TabletFrame frame)
        {
            if (CurrentFrame == frame)
                CollapseCurrentFrame();
            frame.Terminate();
            ActiveFrames.Remove(frame);
            Destroy(frame.gameObject);
        }
        
        private void SetupPinnedDisplays()
        {
            PinnedDisplays = _PinnedDisplaysRoot.GetComponentsInChildren<TabletWidgetDefinitionDisplay>(true).ToList();
            for (var i = 0; i < PinnedDisplays.Count; i++)
            {
                var display = PinnedDisplays[i];
                var definition = _Definition.PinnedWidgets.Count > i ? _Definition.PinnedWidgets[i] : null;
                if (definition == null)
                {
                    display.gameObject.SetActive(false);
                    continue;
                }
                display.SetDefinition(definition);
                display.OnClick += OnWidgetDisplayClicked;
            }
        }
        
        private void OnWidgetDisplayClicked(TabletWidgetDefinition definition)
        {
            if (CurrentFrame != null && CurrentFrame.ViewSource is ReferenceTabletViewSource currentRef && currentRef.Reference == definition.WidgetView)
            {
                CollapseCurrentFrame();
                return;
            }
            
            OpenWidget(definition);
        }
    }
}
