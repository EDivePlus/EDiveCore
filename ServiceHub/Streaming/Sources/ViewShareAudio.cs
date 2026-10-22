// Author: František Holubec
// Created: 20.10.2026

using System;
using System.Threading;

using Cysharp.Threading.Tasks;
using EDIVE.Audio;
using EDIVE.Core;
using LiveKit;
using LiveKit.Proto;
using UnityEngine;

namespace EDIVE.ServiceHub.Streaming.Sources
{
    // Sound of a shared view for web viewers: what the user hears and their mic
    internal class ViewShareAudio
    {
        private readonly Room _room;
        private readonly string _participant;

        private AudioListenerTap _tap;
        private PushAudioSource _hearingSource;
        private LocalAudioTrack _hearingTrack;

        private AudioManager _audioManager;
        private PushAudioSource _micSource;
        private LocalAudioTrack _micTrack;
        private bool _micWanted;

        public bool HasAudio => _hearingTrack != null || _micTrack != null || _micWanted;

        public ViewShareAudio(Room room, string participant)
        {
            _room = room;
            _participant = participant;
        }

        public async UniTask SetHearingAsync(bool enabled, CancellationToken cancellationToken)
        {
            if (enabled == (_hearingTrack != null))
                return;
            if (!enabled)
            {
                StopHearing();
                return;
            }

            var listener = UnityEngine.Object.FindAnyObjectByType<AudioListener>();
            if (listener == null)
                return;

            _tap = listener.GetComponent<AudioListenerTap>() ?? listener.gameObject.AddComponent<AudioListenerTap>();
            // Processing would remove music and effects as noise
            _hearingSource = new PushAudioSource(RtcAudioSourceType.AudioSourceCustom, nativeOptions: new AudioSourceOptions { EchoCancellation = false, NoiseSuppression = false, AutoGainControl = false });
            _tap.AudioRead += _hearingSource.Push;
            _hearingSource.Start();

            // Same name as the video, the web pairs them
            var track = LocalAudioTrack.CreateAudioTrack($"view:{_participant}", _hearingSource, _room);
            if (await PublishAsync(track, TrackSource.SourceScreenshareAudio, cancellationToken))
                _hearingTrack = track;
            else
                StopHearing();
        }

        public void SetMic(bool enabled)
        {
            if (enabled == _micWanted)
                return;
            _micWanted = enabled;
            if (!enabled)
            {
                StopMic();
                return;
            }

            // Voice chat owns the microphone, its frames are reused
            if (AppCore.Services.TryGet(out _audioManager))
                _audioManager.LocalRawAudioFrameReady += OnMicFrame;
        }

        public void Dispose()
        {
            StopHearing();
            _micWanted = false;
            StopMic();
        }

        private void OnMicFrame(Adrenak.UniVoice.AudioFrame frame)
        {
            if (!_micWanted || frame.samples == null || frame.channelCount <= 0)
                return;

            // Format is known only from the first frame
            if (_micSource == null)
            {
                _micSource = new PushAudioSource(RtcAudioSourceType.AudioSourceMicrophone, (uint) frame.frequency, (uint) frame.channelCount);
                _micSource.Start();
                PublishMicAsync().Forget();
            }
            _micSource.Push(Adrenak.UniVoice.Utils.Bytes.BytesToFloats(frame.samples), frame.channelCount, frame.frequency);
        }

        private async UniTaskVoid PublishMicAsync()
        {
            var track = LocalAudioTrack.CreateAudioTrack($"mic:{_participant}", _micSource, _room);
            if (await PublishAsync(track, TrackSource.SourceMicrophone, CancellationToken.None) && _micWanted)
                _micTrack = track;
            else if (_micWanted)
                StopMic();
            else
                _room.LocalParticipant.UnpublishTrack(track, true);
        }

        private async UniTask<bool> PublishAsync(LocalAudioTrack track, TrackSource source, CancellationToken cancellationToken)
        {
            var publish = _room.LocalParticipant.PublishTrack(track, new TrackPublishOptions { Source = source });
            try
            {
                await publish.AsUniTask(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                await publish.AsUniTask();
                if (!publish.IsError)
                    _room.LocalParticipant.UnpublishTrack(track, true);
                throw;
            }
            if (!publish.IsError)
                return true;
            Debug.LogWarning($"[ViewShareAudio] Publish {source} failed.");
            return false;
        }

        private void StopHearing()
        {
            if (_hearingTrack != null && _room.IsConnected)
                _room.LocalParticipant.UnpublishTrack(_hearingTrack, true);
            _hearingTrack = null;
            if (_tap != null && _hearingSource != null)
                _tap.AudioRead -= _hearingSource.Push;
            _hearingSource?.Stop();
            _hearingSource?.Dispose();
            _hearingSource = null;
        }

        private void StopMic()
        {
            if (_audioManager != null)
                _audioManager.LocalRawAudioFrameReady -= OnMicFrame;
            _audioManager = null;
            if (_micTrack != null && _room.IsConnected)
                _room.LocalParticipant.UnpublishTrack(_micTrack, true);
            _micTrack = null;
            _micSource?.Stop();
            _micSource?.Dispose();
            _micSource = null;
        }
    }
}
