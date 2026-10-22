// Author: František Holubec
// Created: 20.10.2026

using System;
using PurrNet.Packing;
using UnityEngine;

namespace EDIVE.ServiceHub.Streaming.Displays
{
    // What a display shows and who put it there
    [Serializable]
    public class StreamScreenState : IPackedAuto
    {
        [SerializeField]
        private string _Source;

        [SerializeField]
        private string _Presenter;

        [SerializeField]
        private string _PresenterName;

        public string Source => _Source;
        public string Presenter => _Presenter;
        public string PresenterName => _PresenterName;
        public bool IsEmpty => string.IsNullOrEmpty(_Source);

        public static StreamScreenState Empty => new(null, null, null);

        public StreamScreenState(string source, string presenter, string presenterName)
        {
            _Source = string.IsNullOrEmpty(source) ? null : source;
            _Presenter = presenter;
            _PresenterName = presenterName;
        }
    }

    public enum StreamScreenView
    {
        Idle,
        Loading,
        Showing,
        Reconnecting
    }

    public enum StreamScreenAllow
    {
        Anyone,
        Managers,
        Nobody
    }
}
