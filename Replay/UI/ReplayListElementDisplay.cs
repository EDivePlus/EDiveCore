// Author: Radim Holub
// Created: 04.12.2025

using System;
using EDIVE.Time.TimeSpanUtils;
using EDIVE.UIElements.RecyclableScroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.Replay.UI
{
    public class ReplayListElementDisplay : RecyclableScrollerItemView
    {
        [SerializeField]
        private TMP_Text _IDText;

        [SerializeField]
        private TimeSpanDisplay _DurationDisplay;
        
        [SerializeField]
        private Button _LoadButton;

        private AReplayRecordMeta _info;
        private Action<AReplayRecordMeta> _onClicked;

        public void SetReplay(AReplayRecordMeta info, Action<AReplayRecordMeta> onClicked)
        {
            _info = info;
            _onClicked = onClicked;

            if (_IDText)
                _IDText.text = info.ID;
            
            if (_DurationDisplay)
                _DurationDisplay.SetTimeSpan(TimeSpan.FromSeconds(info.Duration));
            
            if (_LoadButton) 
                _LoadButton.onClick.AddListener(OnLoadClicked);
        }

        private void OnLoadClicked()
        {
            _onClicked?.Invoke(_info);
        }
    }
}
