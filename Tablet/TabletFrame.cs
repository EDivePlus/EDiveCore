// Author: Michal Petr
// Created: 03.03.2026

using System;
using Cysharp.Threading.Tasks;
using EDIVE.StateHandling.MultiStates;
using EDIVE.StateHandling.ToggleStates;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.Tablet
{
    public class TabletFrame : MonoBehaviour
    {
        [SerializeField]
        private Transform _ViewRoot;

        [SerializeField]
        private bool _IsPersistent;
        
        [SerializeField]
        [HideIf("_IsPersistent")]
        private Button _CloseButton;
        
        [SerializeField]
        private Button _CollapseButton;
        
        [SerializeField]
        [ValidateMultiState(typeof(TabletFrameState))]
        private AMultiState _FrameState;
        
        [SerializeField]
        private AToggleState _LoadingState;
        
        public TabletController Controller { get; private set; }
        public ITabletViewSource ViewSource { get; private set; }
        public ATabletView View { get; private set; }
        public TabletFrameState State { get; set; } = TabletFrameState.Initial;
        public bool IsPersistent => _IsPersistent;
        
        public bool IsLoading { get; private set; }

        public async UniTask Initialize(TabletController controller, ITabletViewSource source = null, ITabletViewContext context = null)
        {
            Controller = controller;
            ViewSource = source;
            SetLoading(true);
            
            if (source == null)
            {
                View = _ViewRoot.GetComponentInChildren<ATabletView>();
                ViewSource = new InstanceTabletViewSource(View);
            }
            else if (source is InstanceTabletViewSource instanceSource)
            {
                View = instanceSource.Instance;
            }
            else if (source is ReferenceTabletViewSource referenceSource)
            {
                var reference = referenceSource.Reference;
                var viewObj = await reference.InstantiateAsync(_ViewRoot);
                View = viewObj.GetComponent<ATabletView>();
            }
            
            if (View == null)
            {
                Debug.LogError($"Failed to initialize frame {name}. No view found.", this);
                SetLoading(false);
                return;
            }

            await View.Initialize(controller, context);
            
            SetLoading(false);

            if (State == TabletFrameState.Open)
            {
                if (View) View.OnOpen();
            }
        }
        
        public void Terminate()
        {
            if (IsPersistent)
            {
                Debug.LogWarning($"Attempted to terminate a persistent frame {name}. Ignoring.", this);
                return;
            }
            
            SetState(TabletFrameState.Terminated);
            if (View) View.OnTerminate();
        }

        private void OnEnable()
        {
            if (_CloseButton) _CloseButton.onClick.AddListener(OnCloseButtonClicked);
            if (_CollapseButton) _CollapseButton.onClick.AddListener(OnCollapseButtonClicked);
        }

        private void OnDisable()
        {
            if (_CloseButton) _CloseButton.onClick.RemoveListener(OnCloseButtonClicked);
            if (_CollapseButton) _CollapseButton.onClick.RemoveListener(OnCollapseButtonClicked);
        }

        public void Open()
        {
            SetState(TabletFrameState.Open);
            if (View) View.OnOpen();
        }
        
        public void Collapse()
        {
            SetState(TabletFrameState.Collapsed);
            if (View) View.OnCollapse();
        }
        
        private void SetState(TabletFrameState newState)
        {
            State = newState;
            if (_FrameState) _FrameState.SetState(newState);
        }
        
        private void SetLoading(bool isLoading)
        {
            IsLoading = isLoading;
            if (_LoadingState) _LoadingState.SetState(isLoading);
        }
        
        private void OnCloseButtonClicked() => Controller.TerminateFrame(this);
        private void OnCollapseButtonClicked() => Controller.CollapseCurrentFrame();
    }

    public enum TabletFrameState
    {
        Initial,
        Open,
        Collapsed,
        Terminated
    }
}
