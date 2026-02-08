// Author: František Holubec
// Created: 08.02.2026

using UnityEngine;

namespace EDIVE.Audio.Replay
{
    /// <summary>
    /// A simple audio source for playing back pre-recorded audio frames.
    /// Uses a linear buffer (not ring buffer) - audio is written sequentially and played back linearly.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class LocalAudioSource : MonoBehaviour
    {
        [Tooltip("Initial buffer size in seconds. Will expand automatically if needed.")]
        [SerializeField] private float _InitialBufferSec = 30f;

        /// <summary>
        /// The <see cref="AudioSource"/> that plays the streaming audio
        /// </summary>
        public AudioSource UnityAudioSource
        {
            get
            {
                if (_source == null)
                {
                    _source = GetComponent<AudioSource>();
                    _source.loop = false; // Linear playback, no loop
                    _source.playOnAwake = false;
                    _source.dopplerLevel = 0f;
                }
                return _source;
            }
        }

        /// <summary>
        /// Total duration of audio written so far (seconds)
        /// </summary>
        public float WrittenDurationSec => _curFrequency > 0 ? (float)_writePosPerChannel / _curFrequency : 0f;

        /// <summary>
        /// Current playback position (seconds)
        /// </summary>
        public float PlaybackPositionSec => UnityAudioSource.time;

        /// <summary>
        /// Whether playback is currently running
        /// </summary>
        public bool IsPlaying => UnityAudioSource.isPlaying;

        #region INTERNAL STATE
        private AudioSource _source;
        private AudioClip _clip;

        private int _curFrequency;
        private int _curChannels;
        private int _clipSamplesPerChannel;
        private int _writePosPerChannel;
        private bool _hasStartedPlaying;
        #endregion

        /// <summary>
        /// Creates a new LocalAudioSource instance
        /// </summary>
        public static LocalAudioSource New(string name = null)
        {
            var go = new GameObject(name ?? "LocalAudioSource");
            go.hideFlags = HideFlags.DontSave;
            DontDestroyOnLoad(go);
            return go.AddComponent<LocalAudioSource>();
        }

        /// <summary>
        /// Feed one frame of audio as bytes (float PCM encoded as bytes).
        /// </summary>
        public void Feed(int frequency, int channels, byte[] samplesAsBytes)
        {
            if (!gameObject.activeInHierarchy) return;
            if (!UnityAudioSource.enabled) return;
            if (samplesAsBytes == null || samplesAsBytes.Length == 0) return;

            int floatCount = samplesAsBytes.Length / sizeof(float);
            float[] floatArray = new float[floatCount];
            System.Buffer.BlockCopy(samplesAsBytes, 0, floatArray, 0, samplesAsBytes.Length);

            FeedInternal(frequency, channels, floatArray);
        }
        
        private void FeedInternal(int frequency, int channels, float[] samples)
        {
            int perChannelSamples = samples.Length / channels;

            // Create or recreate clip if format changed
            if (_clip == null || frequency != _curFrequency || channels != _curChannels)
            {
                CreateClip(frequency, channels);
            }

            // Expand clip if we're running out of space
            if (_writePosPerChannel + perChannelSamples > _clipSamplesPerChannel)
            {
                ExpandClip(frequency, channels);
            }

            // Write samples at write position
            _clip.SetData(samples, _writePosPerChannel);
            _writePosPerChannel += perChannelSamples;

            // Start playing immediately on first feed
            if (!_hasStartedPlaying)
            {
                UnityAudioSource.time = 0f;
                UnityAudioSource.Play();
                _hasStartedPlaying = true;
            }
        }

        private void CreateClip(int frequency, int channels)
        {
            float savedTime = 0f;
            bool wasPlaying = false;

            if (_clip != null)
            {
                savedTime = UnityAudioSource.time;
                wasPlaying = UnityAudioSource.isPlaying;
                Destroy(_clip);
            }

            _curFrequency = frequency;
            _curChannels = channels;
            _clipSamplesPerChannel = Mathf.CeilToInt(_InitialBufferSec * frequency);
            _writePosPerChannel = 0;

            _clip = AudioClip.Create("ReplayClip", _clipSamplesPerChannel, channels, frequency, false);

            // Initialize with silence
            var silence = new float[_clipSamplesPerChannel * channels];
            _clip.SetData(silence, 0);

            UnityAudioSource.clip = _clip;

            if (wasPlaying)
            {
                UnityAudioSource.time = savedTime;
                UnityAudioSource.Play();
            }
        }

        private void ExpandClip(int frequency, int channels)
        {
            float savedTime = UnityAudioSource.time;
            bool wasPlaying = UnityAudioSource.isPlaying;

            // Get existing data
            int oldSize = _clipSamplesPerChannel;
            float[] existingData = new float[oldSize * channels];
            _clip.GetData(existingData, 0);

            // Create new larger clip (double size)
            int newSize = oldSize * 2;
            Destroy(_clip);
            _clip = AudioClip.Create("ReplayClip", newSize, channels, frequency, false);
            _clipSamplesPerChannel = newSize;

            // Copy old data
            _clip.SetData(existingData, 0);

            // Fill rest with silence
            var silence = new float[(newSize - oldSize) * channels];
            _clip.SetData(silence, oldSize);

            UnityAudioSource.clip = _clip;

            // Restore playback position
            if (wasPlaying)
            {
                UnityAudioSource.time = savedTime;
                UnityAudioSource.Play();
            }
        }

        /// <summary>
        /// Stops playback and resets for new recording
        /// </summary>
        public void Stop()
        {
            UnityAudioSource.Stop();
            _hasStartedPlaying = false;
            _writePosPerChannel = 0;
        }

        /// <summary>
        /// Clears buffer and resets state
        /// </summary>
        public void Clear()
        {
            Stop();
            if (_clip != null)
            {
                var silence = new float[_clipSamplesPerChannel * _curChannels];
                _clip.SetData(silence, 0);
            }
        }

        private void OnDestroy()
        {
            if (_clip != null) Destroy(_clip);
        }

        [System.Obsolete("new not allowed. Use LocalAudioSource.New", true)]
        public LocalAudioSource() { }
    }
}
