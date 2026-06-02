// Author: František Holubec
// Created: 23.06.2025

using System.Collections.Generic;
using DG.Tweening;
using EDIVE.StateHandling.ToggleStates;
using EDIVE.UIElements.RecyclableScroller;
using EDIVE.Utils.Activations;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace EDIVE.StagePlay.UI
{
    public class StagePlayDisplay : MonoBehaviour
    {
        [Required]
        [SerializeField]
        private StagePlayController _Controller;
        
        [SerializeField]
        private RecyclableScroller _Scroller;
        
        [SerializeField]
        private StagePlaySegmentDisplay _SpeechDisplayPrefab;
        
        [SerializeField]
        private StagePlaySegmentDisplay _DirectionDisplayPrefab;
        
        [SerializeReference]
        private IActivation _ScrollToCurrentActivation;
        
        [SerializeReference]
        private IActivation _ToggleAutoScrollActivation;
        
        [SerializeField]
        private AToggleState _AutoScrollToggle;

        [SerializeField]
        private TMP_Text _NameText;
        
        [SerializeField]
        private float _AutoScrollScrollerOffset = 0.5f;
        
        [SerializeField]
        private float _AutoScrollCellOffset = 0.5f;
        
        [SerializeField]
        private Ease _AutoScrollEase = Ease.InOutQuad;
        
        [SerializeField]
        private float _AutoScrollTweenTime = 0.3f;
        
        private bool _autoScroll = true;
        private List<StagePlaySegmentDisplayData> _segmentsList;
        
        private StagePlayDefinition _currentDefinition;
        private StagePlayState _currentState;
        
        protected void Awake()
        {
            if (_Controller == null)
                return;
            
            _Scroller.Source = new RecyclableScrollerSource(
                () => _segmentsList?.Count ?? 0,
                GetSegmentSize,
                GetSegmentItemView);
            _Scroller.ItemWillRecycle += OnItemWillRecycle;
            
            if(_AutoScrollToggle)
                _AutoScrollToggle.SetState(_autoScroll);
        }
        
        private void OnEnable()
        {
            _ScrollToCurrentActivation?.RegisterActivationListener(OnScrollToCurrentActivated);
            _ToggleAutoScrollActivation?.RegisterActivationListener(ToggleAutoScroll);
        }

        private void OnDisable()
        {
            _ScrollToCurrentActivation?.UnregisterActivationListener(OnScrollToCurrentActivated);
            _ToggleAutoScrollActivation?.UnregisterActivationListener(ToggleAutoScroll);
        }

        private void OnDestroy()
        {
            _Controller.DefinitionChanged -= UpdateDefinition;
            if (_Scroller != null)
                _Scroller.ItemWillRecycle -= OnItemWillRecycle;
        }

        private void Start()
        {
            if (_Controller == null)
                return;
            
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
            _Scroller.JumpToDataIndex(_currentState.CurrentSegmentIndex, new RecyclableScroller.JumpOptions
            {
                ScrollerOffset = _AutoScrollScrollerOffset,
                ItemOffset = _AutoScrollCellOffset,
                Ease = _AutoScrollEase,
                TweenTime = _AutoScrollTweenTime,
            });
        }
        
        private void OnScrollToCurrentActivated()
        {
            JumpToCurrentSegment();
        }
        
        private static void OnItemWillRecycle(RecyclableScrollerItemView itemView)
        {
            if (itemView is StagePlaySegmentDisplay display)
                display.Clear();
        }
        
        private float GetSegmentSize(int dataIndex)
        {
            var segmentData = _segmentsList[dataIndex];
            return segmentData.Segment.Type switch
            {
                StagePlaySegmentType.Speach => _SpeechDisplayPrefab.CalculateHeight(segmentData),
                StagePlaySegmentType.Direction => _DirectionDisplayPrefab.CalculateHeight(segmentData),
                _ => 0
            };
        }

        private RecyclableScrollerItemView GetSegmentItemView(int dataIndex, int itemIndex)
        {
            var itemView = _segmentsList[dataIndex].Segment.Type switch
            {
                StagePlaySegmentType.Speach => _SpeechDisplayPrefab ? _Scroller.GetItemView(_SpeechDisplayPrefab) as StagePlaySegmentDisplay : null,
                StagePlaySegmentType.Direction => _DirectionDisplayPrefab ? _Scroller.GetItemView(_DirectionDisplayPrefab) as StagePlaySegmentDisplay : null,
                _ => null
            };

            if (!itemView)
                return null;

            itemView.SetData(_segmentsList[dataIndex]);
            return itemView;
        }
    }
}
