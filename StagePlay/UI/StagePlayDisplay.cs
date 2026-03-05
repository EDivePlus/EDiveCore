// Author: František Holubec
// Created: 23.06.2025

using System.Collections.Generic;
using DG.Tweening;
using EDIVE.StateHandling.ToggleStates;
using EDIVE.Utils.Activations;
using EDIVE.XRTools;
using EnhancedUI.EnhancedScroller;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using Tween = DG.Tweening.Tween;

namespace EDIVE.StagePlay.UI
{
    public class StagePlayDisplay : MonoBehaviour, IEnhancedScrollerDelegate
    {
        [Required]
        [SerializeField]
        private StagePlayController _Controller;
        
        [SerializeField]
        private SmoothCameraFollower _CameraFollower;
        
        [SerializeField]
        private bool _OpenOnStart = true;
        
        [SerializeField]
        private float _TweenDuration = 0.3f;

        [SerializeField]
        private EnhancedScroller _Scroller;
        
        [SerializeField]
        private StagePlaySegmentDisplay _SpeechDisplayPrefab;
        
        [SerializeField]
        private StagePlaySegmentDisplay _DirectionDisplayPrefab;
        
        [SerializeReference]
        private IActivation _ScrollToCurrentActivation;
        
        [FormerlySerializedAs("_ToggleAction")]
        [SerializeReference]
        private IActivation _ToggleActivation;
        
        [SerializeReference]
        private IActivation _ToggleAutoScrollActivation;
        
        [SerializeField]
        private AToggleState _AutoScrollToggle;
        
        [SerializeField]
        private Transform _RootTransform;

        [SerializeField]
        private TMP_Text _NameText;
        
        [SerializeField]
        private float _AutoScrollScrollerOffset = 0.5f;
        
        [SerializeField]
        private float _AutoScrollCellOffset = 0.5f;
        
        [SerializeField]
        private EnhancedScroller.TweenType _AutoScrollEase = EnhancedScroller.TweenType.easeInOutQuad;
        
        [SerializeField]
        private float _AutoScrollTweenTime = 0.3f;
        
        [DisableInEditorMode]
        [ShowInInspector]
        public bool IsOpen
        {
            get => _isOpen;
            set => SetOpen(value);
        }
    
        private Tween _animTween;
        private bool _isOpen;
        private bool _autoScroll = true;
        private List<StagePlaySegmentDisplayData> _segmentsList;
        
        private StagePlayDefinition _currentDefinition;
        private StagePlayState _currentState;
        
        protected void Awake()
        {
            if (_Controller == null)
                return;
            
            _Scroller.Delegate = this;
            _Scroller.cellViewWillRecycle = OnCellViewWillRecycle;
            
            if(_AutoScrollToggle)
                _AutoScrollToggle.SetState(_autoScroll);
        }
        
        private void OnEnable()
        {
            _ScrollToCurrentActivation?.RegisterActivationListener(OnScrollToCurrentActivated);
            _ToggleActivation?.RegisterActivationListener(ToggleTablet);
            _ToggleAutoScrollActivation?.RegisterActivationListener(ToggleAutoScroll);
        }

        private void OnDisable()
        {
            _ScrollToCurrentActivation?.UnregisterActivationListener(OnScrollToCurrentActivated);
            _ToggleActivation?.UnregisterActivationListener(ToggleTablet);
            _ToggleAutoScrollActivation?.UnregisterActivationListener(ToggleAutoScroll);
        }

        private void OnDestroy()
        {
            _Controller.DefinitionChanged -= UpdateDefinition;
        }

        private void Start()
        {
            if (_Controller == null)
                return;
            
            if (_OpenOnStart)
                SetOpen(true);
            
            _Controller.DefinitionChanged += UpdateDefinition;
            UpdateDefinition(_Controller.Definition, _Controller.CurrentState);
        }
        
        private void ToggleAutoScroll()
        {
            _autoScroll = !_autoScroll;
            if(_AutoScrollToggle)
                _AutoScrollToggle.SetState(_autoScroll);
            
            if (_autoScroll)
                JumpToCurrentSegment();
        }

        private void UpdateDefinition(StagePlayDefinition definition, StagePlayState state)
        {
            if (definition == null || state == null)
                return;
            
            _currentDefinition = definition;
            if (_currentState != state)
            {
                if (_currentState != null ) 
                    _currentState.CurrentSegmentChanged -= OnCurrentSegmentChanged;
                _currentState = state;
                _currentState.CurrentSegmentChanged += OnCurrentSegmentChanged;
            }
            
            if(_NameText)
                _NameText.text = _currentDefinition.Name;
            
            _segmentsList ??= new List<StagePlaySegmentDisplayData>();
            _segmentsList.Clear();

            if (definition != null)
            {
                var index = 0;
                foreach (var segment in definition.ScriptSegments)
                {
                    var segmentData = new StagePlaySegmentDisplayData(index, segment, definition, state);
                    _segmentsList.Add(segmentData);
                    index++;
                }
            }
            _Scroller.ReloadData(_Scroller.NormalizedScrollPosition);
        }

        private void OnCurrentSegmentChanged(int index)
        {
            if (_autoScroll)
                JumpToCurrentSegment();
        }

        [Button]
        public void JumpToCurrentSegment()
        {
            _Scroller.JumpToDataIndex(
                _currentState.CurrentSegmentIndex, 
                _AutoScrollScrollerOffset,
                _AutoScrollCellOffset,
                true, 
                _AutoScrollEase, 
                _AutoScrollTweenTime);
        }
        
        [Button]
        public void ToggleTablet()
        {
            _isOpen = !_isOpen;
            SetOpen(_isOpen);
        }
        
        private void OnScrollToCurrentActivated()
        {
            JumpToCurrentSegment();
        }
        
        public void SetOpen(bool open, bool immediate = false)
        {
            _animTween?.Kill();
            _isOpen = open;

            if (_CameraFollower != null && open)
            {
                _CameraFollower.Reposition(immediate);
            }
            
            if (_RootTransform)
            {
                var newScale = open ? Vector3.one : Vector3.zero;
                if (immediate)
                    _RootTransform.localScale = newScale;
                else
                    _animTween = _RootTransform.DOScale(newScale, _TweenDuration).SetEase(Ease.InOutQuad);
            }
        }
        
        private static void OnCellViewWillRecycle(EnhancedScrollerCellView cellView)
        {
            if (cellView is StagePlaySegmentDisplay display) 
                display.Clear();
        }
        
        public int GetNumberOfCells(EnhancedScroller scroller) => _segmentsList?.Count ?? 0;
        
        public float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
        {
            var segmentData = _segmentsList[dataIndex];
            return segmentData.Segment.Type switch
            {
                StagePlaySegmentType.Speach => _SpeechDisplayPrefab.CalculateHeight(segmentData),
                StagePlaySegmentType.Direction => _DirectionDisplayPrefab.CalculateHeight(segmentData),
                _ => 0
            };
        }

        public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
        {
            var cellView = _segmentsList[dataIndex].Segment.Type switch
            {
                StagePlaySegmentType.Speach => _SpeechDisplayPrefab ? _Scroller.GetCellView(_SpeechDisplayPrefab) as StagePlaySegmentDisplay : null,
                StagePlaySegmentType.Direction => _DirectionDisplayPrefab ? _Scroller.GetCellView(_DirectionDisplayPrefab) as StagePlaySegmentDisplay : null,
                _ => null
            };
            
            if (!cellView)
                return null;
            
            cellView.SetData(_segmentsList[dataIndex]);
            return cellView;
        }
    }
}
