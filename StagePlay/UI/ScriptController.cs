// Author: František Holubec
// Created: 23.06.2025

using System;
using System.Collections.Generic;
using DG.Tweening;
using EDIVE.StateHandling.ToggleStates;
using EDIVE.Utils.Activations;
using EDIVE.XRTools;
using EnhancedUI.EnhancedScroller;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Tween = DG.Tweening.Tween;

namespace EDIVE.StagePlay.UI
{
    public class ScriptController : MonoBehaviour, IEnhancedScrollerDelegate
    {
        [SerializeField]
        private StagePlayDefinition _Definition;
        
        [SerializeField]
        private SmoothCameraFollower _CameraFollower;
        
        [SerializeField]
        private bool _OpenOnStart = true;
        
        [SerializeField]
        private float _TweenDuration = 0.3f;

        [SerializeField]
        private EnhancedScroller _Scroller;

        [SerializeField]
        private LineScriptSegmentDisplay _LineDisplayPrefab;
        
        [SerializeField]
        private DirectionScriptSegmentDisplay _DirectionDisplayPrefab;
        
        [SerializeReference]
        private IActivation _ScrollToCurrentActivation;
        
        [SerializeReference]
        private IActivation _IncrementSegmentActivation;

        [SerializeField]
        private TMP_Text _NameText;
        
        [DisableInEditorMode]
        [ShowInInspector]
        public bool IsOpen
        {
            get => _isOpen;
            set => SetOpen(value);
        }
        
        public StagePlayDefinition Definition => _Definition;
        
        public event Action<StagePlayDefinition> DefinitionChanged;
        public event Action<int> CurrentSegmentChanged;
        
        private Tween _animTween;
        private bool _isOpen;
        private SharedSegmentData _sharedData;
        private List<ASegmentDisplayData> _segmentsList;
        
        protected void Awake()
        {
            _Scroller.Delegate = this;
            _Scroller.cellViewWillRecycle = OnCellViewWillRecycle;
            _ScrollToCurrentActivation?.RegisterActivationListener(OnScrollToCurrentActivated);
            _IncrementSegmentActivation?.RegisterActivationListener(OnIncrementSegmentActivated);
        }

        private void Start()
        {
            if (_OpenOnStart)
                SetOpen(true);
            
            if (_Definition != null)
                InitializeDefinition();
        }

        public void SetDefinition(StagePlayDefinition definition)
        {
            if (_Definition == definition)
                return;

            _Definition = definition;
            DefinitionChanged?.Invoke(_Definition);
            InitializeDefinition();
        }

        public void IncrementCurrentSegment()
        {
            var newIndex = Mathf.Clamp(_sharedData.CurrentSegmentIndex + 1, 0, _Definition.ScriptSegments.Count - 1) ;
            SetCurrentSegment(newIndex);
        }
        
        public void SetCurrentSegment(int index)
        {
            _sharedData.CurrentSegmentIndex = index;
        }

        private void InitializeDefinition()
        {
            if(_NameText)
                _NameText.text = _Definition.Name;
            
            if (_sharedData != null) 
                _sharedData.CurrentSegmentChanged -= OnDataCurrentSegmentChanged;
            _sharedData = new SharedSegmentData();
            _sharedData.CurrentSegmentChanged += OnDataCurrentSegmentChanged;
            
            RefreshScroller();
        }

        private void OnDataCurrentSegmentChanged(int index)
        {
            CurrentSegmentChanged?.Invoke(index);
        }

        private void RefreshScroller()
        {
            _segmentsList ??= new List<ASegmentDisplayData>();
            _segmentsList.Clear();

            var index = 0;
            foreach (var segment in _Definition.ScriptSegments)
            {
                ASegmentDisplayData segmentData = segment switch
                {
                    LineScriptSegment lineSegment => new SegmentDisplayData<LineScriptSegment>(index, lineSegment, _sharedData),
                    DirectionScriptSegment directionSegment => new SegmentDisplayData<DirectionScriptSegment>(index, directionSegment, _sharedData),
                    _ => null
                };

                if (segmentData == null) 
                    continue;
                
                _segmentsList.Add(segmentData);
                index++;
            }
            _Scroller.ReloadData(_Scroller.NormalizedScrollPosition);
        }

        [Button]
        public void JumpToCurrentSegment()
        {
            _Scroller.JumpToDataIndex(_sharedData.CurrentSegmentIndex, 0.5f, 0.5f, true, EnhancedScroller.TweenType.easeOutBack, 0.3f);
        }
        
        [Button]
        public void ToggleTablet()
        {
            _isOpen = !_isOpen;
            SetOpen(_isOpen);
        }
        
        private void OnIncrementSegmentActivated()
        {
            IncrementCurrentSegment();
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
            
            var newScale = open ? Vector3.one : Vector3.zero;
            if (immediate)
                transform.localScale = newScale;
            else
                _animTween = transform.DOScale(newScale, _TweenDuration).SetEase(Ease.InOutQuad);
        }
        
        private static void OnCellViewWillRecycle(EnhancedScrollerCellView cellView)
        {
            if (cellView is AScriptSegmentDisplay display) 
                display.Clear();
        }
        
        public int GetNumberOfCells(EnhancedScroller scroller) => _segmentsList?.Count ?? 0;
        
        public float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
        {
            var rect = _segmentsList[dataIndex] switch
            {
                SegmentDisplayData<LineScriptSegment> => (RectTransform) _LineDisplayPrefab?.transform,
                SegmentDisplayData<DirectionScriptSegment> => (RectTransform) _DirectionDisplayPrefab?.transform,
                _ => null
            };
            
            if (rect != null) return rect.rect.height;
            return 0;
        }

        public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
        {
            var cellView = _segmentsList[dataIndex] switch
            {
                SegmentDisplayData<LineScriptSegment> _ => _LineDisplayPrefab ? _Scroller.GetCellView(_LineDisplayPrefab) as AScriptSegmentDisplay : null,
                SegmentDisplayData<DirectionScriptSegment> _ => _DirectionDisplayPrefab ? _Scroller.GetCellView(_DirectionDisplayPrefab) as AScriptSegmentDisplay : null,
                _ => null
            };

            if (!cellView)
                return null;
            
            cellView.SetData(_segmentsList[dataIndex]);
            return cellView;
        }
    }
}
