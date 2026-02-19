// Author: František Holubec
// Created: 18.02.2026

using System;

namespace EDIVE.StagePlay
{
    public class StagePlayState : IDisposable
    {
        private int _currentSegmentIndex;
        public int CurrentSegmentIndex
        {
            get => _currentSegmentIndex;
            set
            {
                if (_currentSegmentIndex == value)
                    return;
                _currentSegmentIndex = value;
                CurrentSegmentChanged?.Invoke(_currentSegmentIndex);
            }
        }
        
        public string LocalCharacterName { get; set; }

        public event Action<int> CurrentSegmentChanged;

        public void Dispose()
        {
            CurrentSegmentChanged = null;
        }
    }
}
