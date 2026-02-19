// Author: František Holubec
// Created: 23.06.2025

using EDIVE.StateHandling.MultiStates;
using EnhancedUI.EnhancedScroller;
using UnityEngine;

namespace EDIVE.StagePlay.UI
{
    public abstract class APlaySegmentDisplay : EnhancedScrollerCellView
    {
        public abstract void SetData(ASegmentDisplayData data);
        public abstract void Clear();
    } 
    
    public abstract class APlaySegmentDisplay<TSegment> : APlaySegmentDisplay 
        where TSegment : APlaySegment
    {
        [SerializeField]
        [ValidateMultiState(typeof(PlaySegmentState))]
        private AMultiState _State;

        private PlaySegmentState? _currentState;
        
        public SegmentDisplayData<TSegment> Data { get; private set; }
        
        public sealed override void SetData(ASegmentDisplayData data)
        {
            if (data is not SegmentDisplayData<TSegment>  typedData)
                return;
            
            SetData(typedData);
        }

        protected virtual void SetData(SegmentDisplayData<TSegment> data)
        {
            if (data == null)
                return;
            
            // unsubscribe previous if any
            if (Data != null)
                Data.SharedData.CurrentSegmentChanged -= OnCurrentSegmentChanged;

            Data = data;
            Data.SharedData.CurrentSegmentChanged += OnCurrentSegmentChanged;
            
            RefreshState();
        }
        
        public sealed override void Clear()
        {
            if (Data != null) 
                Data.SharedData.CurrentSegmentChanged -= OnCurrentSegmentChanged;
            
            Data = null;
            _currentState = null;
        }
        
        private void OnCurrentSegmentChanged(int index)
        {
            RefreshState();
        }

        private void RefreshState()
        {
            if (Data == null) return;

            var currentIndex = Data.SharedData.CurrentSegmentIndex;

            var newState = Data.Index.CompareTo(currentIndex) switch
            {
                < 0 => PlaySegmentState.Previous,
                0 => PlaySegmentState.Current,
                > 0 => PlaySegmentState.Upcoming
            };

            // Check if the current segment is owned
            if (newState == PlaySegmentState.Current)
            {
                var localName = Data.SharedData.LocalCharacterName;
                if (!string.IsNullOrEmpty(localName) && Data.Segment.IsOwnedByCharacter(localName))
                {
                    newState = PlaySegmentState.OwnedCurrent;
                }
            }

            if (_currentState == newState)
                return;
            
            _currentState = newState;
            if(_State)
                _State.SetState(newState);
        }
    }
}
