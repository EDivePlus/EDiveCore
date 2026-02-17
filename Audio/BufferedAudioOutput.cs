// Author: František Holubec
// Created: 08.02.2026

using System.Collections.Generic;
using Adrenak.UniVoice;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class BufferedAudioOutput : MonoBehaviour
    {
        [Tooltip("How much audio to buffer before starting playback (ms).")]
        [SuffixLabel("ms", true)]
        [SerializeField] private int _InitialBufferSize = 500;
        
        [SuffixLabel("ms", true)]
        [SerializeField] private int _RingBufferSize = 60000;

        public int InitialBufferSize => _InitialBufferSize;
        public bool IsPlaying => _audioSource.isPlaying;
        public bool PlaybackEnabled { get; private set; }
        
        private AudioSource _audioSource;
        private AudioClip _clip;
        private int _frequency;
        private int _channels;
        private int _clipSamplesPerChannel;
        private int _writePosPerChannel;
        private bool _isBuffering = true;
        private float _bufferedDuration;
        private readonly Queue<BufferedFrame> _frameQueue = new();
        private long _lastFrameEndTimestamp;
        private double _frameDuration;
        private bool _hasReceivedFirstFrame;

        private struct BufferedFrame
        {
            public long Timestamp;
            public float[] Samples;
        }
        
        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.loop = false;
            _audioSource.playOnAwake = false;
            _audioSource.dopplerLevel = 0f;
        }
        
        /// <summary>
        /// Enable or disable playback. When enabled, playback will start once the initial buffer is filled.
        /// </summary>
        public void SetPlaybackEnabled(bool state)
        {
            PlaybackEnabled = state;
            
            // If enabling playback and we have enough buffered data, start immediately
            if (state && _isBuffering && _bufferedDuration >= _InitialBufferSize)
            {
                StartPlayback();
            }
            
            // If disabling playback while playing, stop
            if (!state && IsPlaying)
            {
                Stop();
            }
        }
        
        public void Feed(AudioFrame frame)
        {
            Feed(frame.frequency, frame.channelCount, frame.samples, frame.timestamp);
        }
        
        public void Feed(int frequency, int channels, byte[] samples, long timestamp)
        {
            if (samples == null || samples.Length == 0) 
                return;

            Feed(frequency, channels, Adrenak.UniVoice.Utils.Bytes.BytesToFloats(samples), timestamp);
        }
        
        public void Feed(int frequency, int channels, float[] samples, long timestamp)
        {
            if (samples == null || samples.Length == 0) 
                return;
            
            // Calculate frame duration in ms
            var perChannelSamples = samples.Length / channels;
            var frameDurationMs = (float)perChannelSamples / frequency * 1000f;
            _frameDuration = frameDurationMs;

            // Initialize clip if needed
            if (_clip == null || frequency != _frequency || channels != _channels)
            {
                InitializeClip(frequency, channels);
            }

            // Initialize timestamp tracking on first valid timestamp
            if (!_hasReceivedFirstFrame && timestamp >= 0)
            {
                _lastFrameEndTimestamp = timestamp;
                _hasReceivedFirstFrame = true;
            }

            // Add frame to queue
            _frameQueue.Enqueue(new BufferedFrame { Timestamp = timestamp, Samples = samples });
            _bufferedDuration += frameDurationMs;

            // If buffering and playback is enabled, check if we have enough to start
            if (_isBuffering && PlaybackEnabled && _bufferedDuration >= _InitialBufferSize)
            {
                StartPlayback();
            }

            // If playing, flush the queue to the ring buffer
            if (IsPlaying)
            {
                FlushQueue();
            }
        }

        private void InitializeClip(int frequency, int channels)
        {
            _frequency = frequency;
            _channels = channels;

            // Calculate ring buffer size (convert ms to seconds for sample calculation)
            _clipSamplesPerChannel = Mathf.CeilToInt((_RingBufferSize / 1000f) * frequency);

            // Destroy old clip
            if (_clip != null)
                Destroy(_clip);

            // Create new clip
            _clip = AudioClip.Create("Recording", _clipSamplesPerChannel, channels, frequency, false);

            // Initialize with silence
            var silence = new float[_clipSamplesPerChannel * channels];
            _clip.SetData(silence, 0);

            _audioSource.clip = _clip;

            // Reset state
            _writePosPerChannel = 0;
            _isBuffering = true;
            _bufferedDuration = 0f;
            _hasReceivedFirstFrame = false;
            _frameQueue.Clear();
        }

        private void StartPlayback()
        {
            _isBuffering = false;

            // Flush all buffered frames to the ring buffer
            FlushQueue();

            // Start playback from the beginning
            _audioSource.time = 0f;
            _audioSource.Play();
        }

        private void FlushQueue()
        {
            // Threshold: if gap is more than 1.5x frame duration, it's a real VAD gap
            var gapThresholdMs = _frameDuration * 1.5;

            while (_frameQueue.Count > 0)
            {
                var frame = _frameQueue.Dequeue();

                // Calculate frame duration for bookkeeping
                var perChannelSamples = frame.Samples.Length / _channels;
                var frameDurationMs = (float)perChannelSamples / _frequency * 1000f;
                _bufferedDuration -= frameDurationMs;

                // Check for VAD gap (only if we have valid timestamps)
                if (frame.Timestamp >= 0 && _hasReceivedFirstFrame)
                {
                    var gapMs = frame.Timestamp - _lastFrameEndTimestamp;

                    // If there's a significant gap, insert silence
                    if (gapMs > gapThresholdMs)
                    {
                        var gapSamplesPerChannel = (int)((gapMs / 1000.0) * _frequency);
                        WriteSilence(gapSamplesPerChannel);
                    }

                    // Update expected end timestamp for next frame
                    _lastFrameEndTimestamp = frame.Timestamp + (long)_frameDuration;
                }

                // Write the audio frame to ring buffer
                WriteToBuffer(frame.Samples);
            }
        }

        private void WriteSilence(int samplesPerChannel)
        {
            // Just advance write position - buffer is already initialized with silence
            _writePosPerChannel += samplesPerChannel;
            
            // Clamp to buffer size (don't wrap for linear playback)
            if (_writePosPerChannel > _clipSamplesPerChannel)
                _writePosPerChannel = _clipSamplesPerChannel;
        }

        private void WriteToBuffer(float[] samples)
        {
            var perChannelSamples = samples.Length / _channels;

            // Don't write past the end of the buffer
            if (_writePosPerChannel + perChannelSamples > _clipSamplesPerChannel)
            {
                // Truncate if we're at the end
                var available = _clipSamplesPerChannel - _writePosPerChannel;
                if (available <= 0) return;
                
                // Write only what fits
                var truncated = new float[available * _channels];
                System.Array.Copy(samples, 0, truncated, 0, truncated.Length);
                _clip.SetData(truncated, _writePosPerChannel);
                _writePosPerChannel = _clipSamplesPerChannel;
                return;
            }

            _clip.SetData(samples, _writePosPerChannel);
            _writePosPerChannel += perChannelSamples;
        }
        
        public void Stop()
        {
            _audioSource.Stop();
            _frameQueue.Clear();
            _isBuffering = true;
            _bufferedDuration = 0f;
            _writePosPerChannel = 0;
            _hasReceivedFirstFrame = false;
            PlaybackEnabled = false;

            // Clear the clip with silence
            if (_clip != null && _clipSamplesPerChannel > 0 && _channels > 0)
            {
                var silence = new float[_clipSamplesPerChannel * _channels];
                _clip.SetData(silence, 0);
            }
        }

        private void OnDestroy()
        {
            if (_clip != null)
                Destroy(_clip);
        }
    }
}
