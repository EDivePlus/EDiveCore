// Author: Michal Petr
// Created: 03.03.2026

using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using EDIVE.Core.Services;
using EDIVE.NativeUtils;
using UnityEngine;

namespace EDIVE.MenuScreen
{
    public class MenuScreenController : AServiceBehaviour<MenuScreenController>
    {
        [SerializeField]
        private MenuScreenDefinition _Definition;
        
        [SerializeField]
        private Transform _FrameRoot;
        
        [SerializeField]
        private MenuScreenFrame _FramePrefab;
        
        [SerializeField]
        private Transform _PinnedDisplaysRoot;
        
        public MenuScreenDefinition Definition => _Definition;

        private MenuScreenFrame CurrentFrame { get; set; }
        private List<MenuScreenFrame> ActiveFrames { get; set; } = new();
        private List<WidgetDefinitionDisplay> PinnedDisplays { get; set; } = new();

        private void Awake()
        {
            SetupPinnedDisplays();
            _FrameRoot.GetComponentsInChildren<MenuScreenFrame>().ForEach(persistentFrame =>
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
        
        private void Start()
        {
            if (_Definition.DefaultWidget)
                OpenWidget(_Definition.DefaultWidget);
        }

        public void OpenWidget(WidgetDefinition definition, IViewContext context = null)
        {
            OpenView(new ReferenceViewSource(definition.WidgetView), context);
        }

        private void OpenView(IViewSource view, IViewContext context = null)
        {
            if (TryOpenActiveFrame(view, context))
                return;

            CreateNewFrame(view, context);
        }
        
        private bool TryGetActiveFrame(IViewSource view, out MenuScreenFrame frame)
        {
            frame = ActiveFrames.Find(f => Equals(f.ViewSource, view));
            return frame != null;
        }
        
        private bool TryOpenActiveFrame(IViewSource view, IViewContext context = null)
        {
            if (!TryGetActiveFrame(view, out var frame)) 
                return false;
            OpenFrame(frame, context);
            return true;
        }
        
        private void CreateNewFrame(IViewSource view, IViewContext context = null, bool open = true)
        {
            var newFrame = Instantiate(_FramePrefab, _FrameRoot);
            // todo set frame name to recognize the view
            newFrame.Initialize(this, view, context).Forget();
            ActiveFrames.Add(newFrame);
            if (open) OpenFrame(newFrame);
        }
        
        public void OpenFrame(MenuScreenFrame frame, IViewContext context = null)
        {
            CollapseCurrentFrame();
            CurrentFrame = frame;
            CurrentFrame.gameObject.SetActive(true);
            frame.Open(context);
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
        
        public void TerminateFrame(MenuScreenFrame frame)
        {
            if (CurrentFrame == frame)
                CollapseCurrentFrame();
            frame.Terminate();
            ActiveFrames.Remove(frame);
            Destroy(frame.gameObject);
        }
        
        private void SetupPinnedDisplays()
        {
            PinnedDisplays = _PinnedDisplaysRoot.GetComponentsInChildren<WidgetDefinitionDisplay>(true).ToList();
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
        
        private void OnWidgetDisplayClicked(WidgetDefinition definition)
        {
            // todo get rid of pattern matching ?
            if (CurrentFrame != null && CurrentFrame.ViewSource is ReferenceViewSource currentRef && currentRef.Reference == definition.WidgetView)
            {
                CollapseCurrentFrame();
                return;
            }
            
            OpenWidget(definition);
        }
    }
}
