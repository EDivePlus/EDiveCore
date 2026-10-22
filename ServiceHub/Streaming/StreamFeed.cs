// Author: František Holubec
// Created: 20.10.2026

using System;
using System.Collections.Generic;
using LiveKit;
using UnityEngine;

namespace EDIVE.ServiceHub.Streaming
{
    // Video and sound of one source, shared by everything showing it
    public class StreamFeed
    {
        private readonly MonoBehaviour _runner;
        private readonly Dictionary<AudioSource, AudioStream> _audioOutputs = new();

        private RemoteVideoTrack _videoTrack;
        private VideoStream _videoStream;
        private Coroutine _videoUpdate;
        private RemoteAudioTrack _audioTrack;
        private RenderTexture _lastFrame;
        private bool _hadFrame;
        private readonly HashSet<StreamFeedLoss> _losses = new();

        public string SourceId { get; }
        internal int RefCount { get; set; }

        // Live video, or the last frame while the source is gone
        public Texture Texture => _videoStream?.Texture != null ? _videoStream.Texture : _lastFrame;
        public bool HasAudio => _audioTrack != null;

        public StreamFeedStatus Status
        {
            get
            {
                if (!_hadFrame)
                    return StreamFeedStatus.Waiting;
                var live = _videoStream?.Texture != null && _losses.Count == 0;
                return live ? StreamFeedStatus.Live : StreamFeedStatus.Reconnecting;
            }
        }

        public event Action<StreamFeed> Changed;

        internal StreamFeed(string sourceId, MonoBehaviour runner)
        {
            SourceId = sourceId;
            _runner = runner;
        }

        public void AddAudioOutput(AudioSource output)
        {
            if (output == null || _audioOutputs.ContainsKey(output))
                return;
            _audioOutputs[output] = _audioTrack != null ? new AudioStream(_audioTrack, output) : null;
        }

        public void RemoveAudioOutput(AudioSource output)
        {
            if (output == null || !_audioOutputs.Remove(output, out var stream))
                return;
            stream?.Dispose();
        }

        internal void SetTrack(IRemoteTrack track)
        {
            switch (track)
            {
                case RemoteVideoTrack video when video != _videoTrack:
                    StopVideo();
                    _losses.Remove(StreamFeedLoss.Muted);
                    _losses.Remove(StreamFeedLoss.PresenterLost);
                    _videoTrack = video;
                    _videoStream = new VideoStream(video);
                    _videoStream.TextureReceived += OnTextureReceived;
                    _videoStream.Start();
                    _videoUpdate = _runner.StartCoroutine(_videoStream.Update());
                    break;
                case RemoteAudioTrack audio when audio != _audioTrack:
                    StopAudio();
                    _audioTrack = audio;
                    foreach (var output in new List<AudioSource>(_audioOutputs.Keys))
                        _audioOutputs[output] = new AudioStream(audio, output);
                    break;
                default:
                    return;
            }
            Changed?.Invoke(this);
        }

        internal void RemoveTrack(IRemoteTrack track)
        {
            if (track == _videoTrack)
                StopVideo();
            else if (track == _audioTrack)
                StopAudio();
            else
                return;
            Changed?.Invoke(this);
        }

        // Left the room, waits for the tracks again
        internal void ClearTracks()
        {
            if (_videoTrack == null && _audioTrack == null)
                return;
            StopVideo();
            StopAudio();
            Changed?.Invoke(this);
        }

        internal void Dispose()
        {
            StopVideo();
            StopAudio();
            _audioOutputs.Clear();
            if (_lastFrame != null)
                UnityEngine.Object.Destroy(_lastFrame);
            _lastFrame = null;
            Changed = null;
        }

        private void OnTextureReceived(Texture texture)
        {
            _hadFrame = true;
            Changed?.Invoke(this);
        }

        // Signals that the source is gone or paused while its track may still exist
        internal void SetLoss(StreamFeedLoss loss, bool lost)
        {
            if (lost ? _losses.Add(loss) : _losses.Remove(loss))
                Changed?.Invoke(this);
        }

        private void KeepLastFrame(Texture texture)
        {
            if (texture == null)
                return;
            if (_lastFrame == null || _lastFrame.width != texture.width || _lastFrame.height != texture.height)
            {
                if (_lastFrame != null)
                    UnityEngine.Object.Destroy(_lastFrame);
                _lastFrame = new RenderTexture(texture.width, texture.height, 0);
            }
            Graphics.Blit(texture, _lastFrame);
        }

        private void StopVideo()
        {
            if (_videoUpdate != null && _runner != null)
                _runner.StopCoroutine(_videoUpdate);
            _videoUpdate = null;
            if (_videoStream != null)
            {
                KeepLastFrame(_videoStream.Texture);
                _videoStream.TextureReceived -= OnTextureReceived;
                _videoStream.Stop();
                _videoStream.Dispose();
            }
            _videoStream = null;
            _videoTrack = null;
        }

        private void StopAudio()
        {
            foreach (var output in new List<AudioSource>(_audioOutputs.Keys))
            {
                _audioOutputs[output]?.Dispose();
                _audioOutputs[output] = null;
            }
            _audioTrack = null;
        }
    }

    public enum StreamFeedStatus
    {
        Waiting,
        Live,
        Reconnecting
    }

    internal enum StreamFeedLoss
    {
        Muted,
        PresenterLost,
        RoomReconnecting
    }
}
