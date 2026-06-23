// Author: Michal Petr
// Created: 03.03.2026

using Cysharp.Threading.Tasks;
using EDIVE.StateHandling.MultiStates;
using EDIVE.StateHandling.ToggleStates;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace EDIVE.MenuScreen
{
    public class MenuScreenFrame : MonoBehaviour
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
        [ValidateMultiState(typeof(FrameState))]
        private AMultiState _FrameState;
        
        [SerializeField]
        private AToggleState _LoadingState;

        public MenuScreenController Controller { get; private set; }
        public IViewSource ViewSource { get; private set; }
        public WidgetView View { get; private set; }
        public FrameState State { get; set; } = FrameState.Initial;
        public bool IsPersistent => _IsPersistent;
        
        private AsyncOperationHandle<GameObject> _asyncViewHandle;
        
        public bool IsLoading { get; private set; }

        public async UniTask Initialize(MenuScreenController controller, IViewSource source = null, IViewContext context = null)
        {
            Controller = controller;
            ViewSource = source;
            SetLoading(true);
            
            if (source == null)
            {
                View = _ViewRoot.GetComponentInChildren<WidgetView>();
                ViewSource = new InstanceViewSource(View);
            }
            // todo get rid of pattern matching, mae universal method in source ?
            else if (source is InstanceViewSource instanceSource)
            {
                View = instanceSource.Instance;
            }
            else if (source is PrefabViewSource prefabSource)
            {
                var prefab = prefabSource.Prefab;
                var viewObj = Instantiate(prefab, _ViewRoot);
                View = viewObj.GetComponent<WidgetView>();
            }
            else if (source is AddressablePrefabViewSource referenceSource)
            {
                var reference = referenceSource.Reference;
                _asyncViewHandle = reference.InstantiateAsync(_ViewRoot);
                var viewObj = await _asyncViewHandle;
                View = viewObj.GetComponent<WidgetView>();
            }
            
            if (View == null)
            {
                Debug.LogError($"Failed to initialize frame {name}. No view found.", this);
                SetLoading(false);
                return;
            }

            await View.Initialize(controller, context);
            
            SetLoading(false);

            if (State == FrameState.Open)
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
            
            _asyncViewHandle.Release();
            
            SetState(FrameState.Terminated);
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

        public void Open(IViewContext context = null)
        {
            SetState(FrameState.Open);
            
            if (!View) 
                return;
            
            View.CurrentContext = context;
            View.OnOpen();
        }
        
        public void Collapse()
        {
            SetState(FrameState.Collapsed);
            if (View) View.OnCollapse();
        }
        
        private void SetState(FrameState newState)
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

    public enum FrameState
    {
        Initial,
        Open,
        Collapsed,
        Terminated
    }
}
