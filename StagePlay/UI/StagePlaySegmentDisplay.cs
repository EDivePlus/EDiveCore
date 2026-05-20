// Author: František Holubec
// Created: 23.06.2025

using EDIVE.StateHandling.MultiStates;
using EnhancedUI.EnhancedScroller;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace EDIVE.StagePlay.UI
{
    internal class StagePlaySegmentDisplay : EnhancedScrollerCellView
    {
        [SerializeField]
        private TMP_Text _LineText;

        [SerializeField]
        private TMP_Text _CharactersText;
        
        [SerializeField]
        private bool _ColorizeCharacterMentions;
        
        [SerializeField]
        [ValidateMultiState(typeof(StagePlaySegmentState))]
        private AMultiState _State;
        
        private StagePlaySegmentState? _currentState;
        public StagePlaySegmentDisplayData Data { get; private set; }
        
        public void SetData(StagePlaySegmentDisplayData data)
        {
            if (Data != null)
                Data.SharedData.CurrentSegmentChanged -= OnCurrentSegmentChanged;
            Data = data;
            Data.SharedData.CurrentSegmentChanged += OnCurrentSegmentChanged;

            var sharedData = data.Definition.SharedData;
            if (_CharactersText != null)
            {
                _CharactersText.text = sharedData != null
                    ? sharedData.ColorizeCharacters(data.Segment.Characters)
                    : data.Segment.Characters;
                if (data.Definition.Font != null)
                    _CharactersText.font = data.Definition.Font;
            }
            
            if (_LineText != null)
            {
                _LineText.text = _ColorizeCharacterMentions && sharedData != null
                    ? sharedData.ColorizeCharacterMentions(data.Segment.Line)
                    : data.Segment.Line;
                if (data.Definition.Font != null)
                    _LineText.font = data.Definition.Font;
            }
            
            RefreshState();
        }
        
        public void Clear()
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
                < 0 => StagePlaySegmentState.Previous,
                0 => StagePlaySegmentState.Current,
                > 0 => StagePlaySegmentState.Upcoming
            };
            
            if (_currentState == newState)
                return;
            
            _currentState = newState;
            if(_State)
                _State.SetState(newState);
        }
        
        public float CalculateHeight(StagePlaySegmentDisplayData data)
        {
            return CalculateHeight(data.Segment.Line, data.Definition.Font);
        }
        
        [Button]
        public float CalculateHeight(string text, TMP_FontAsset font)
        {
            var rectTr = _LineText.rectTransform;
            var width = rectTr.rect.width;
            var originalFont = _LineText.font;
            if (font != null)
                _LineText.font = font;
            var height = _LineText.GetPreferredValues(text, width, Mathf.Infinity).y;
            _LineText.font = originalFont;
            return height + rectTr.offsetMin.y - rectTr.offsetMax.y;
        }
    }
}
