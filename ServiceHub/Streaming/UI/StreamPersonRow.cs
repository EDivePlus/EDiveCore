// Author: František Holubec
// Created: 21.10.2026

using System;
using EDIVE.UIElements;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.ServiceHub.Streaming.UI
{
    // One person in the Streams window or a display picker: Watch when live, Ask otherwise
    public class StreamPersonRow : MonoBehaviour
    {
        public enum RowAction
        {
            None,
            Watch,
            Ask,
            Waiting
        }

        [SerializeField]
        private ProfilePictureDisplay _Avatar;

        [SerializeField]
        private TMP_Text _NameText;

        [SerializeField]
        private TMP_Text _InfoText;

        [SerializeField]
        private Button _WatchButton;

        [SerializeField]
        private Button _AskButton;

        [SerializeField]
        private GameObject _WaitingPill;

        [Tooltip("Optional, the whole row watches when live")]
        [SerializeField]
        private Button _RowButton;

        [SerializeField]
        private GameObject _LiveBadge;

        [Tooltip("Shown on every other row")]
        [SerializeField]
        private Graphic _Background;

        private Action _clicked;

        private void Awake()
        {
            if (_WatchButton) _WatchButton.onClick.AddListener(OnClicked);
            if (_AskButton) _AskButton.onClick.AddListener(OnClicked);
            if (_RowButton) _RowButton.onClick.AddListener(OnClicked);
        }

        private void OnDestroy()
        {
            if (_WatchButton) _WatchButton.onClick.RemoveListener(OnClicked);
            if (_AskButton) _AskButton.onClick.RemoveListener(OnClicked);
            if (_RowButton) _RowButton.onClick.RemoveListener(OnClicked);
        }

        public void Set(string displayName, string info, RowAction action, Action clicked)
        {
            _clicked = clicked;
            if (_Avatar) _Avatar.SetProfilePictureFromName(string.IsNullOrEmpty(displayName) ? "?" : displayName);
            if (_NameText) _NameText.text = displayName;
            if (_InfoText)
            {
                _InfoText.text = info;
                _InfoText.gameObject.SetActive(!string.IsNullOrEmpty(info));
            }
            if (_WatchButton) _WatchButton.gameObject.SetActive(action == RowAction.Watch);
            if (_AskButton) _AskButton.gameObject.SetActive(action == RowAction.Ask);
            if (_WaitingPill) _WaitingPill.SetActive(action == RowAction.Waiting);
            if (_RowButton) _RowButton.interactable = action == RowAction.Watch;
            if (_LiveBadge) _LiveBadge.SetActive(action == RowAction.Watch);
        }

        public void SetAlternate(bool alternate)
        {
            if (_Background) _Background.enabled = alternate;
        }

        private void OnClicked() => _clicked?.Invoke();
    }
}
