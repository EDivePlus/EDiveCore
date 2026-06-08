using System;
using System.Diagnostics;
using UnityEngine;

namespace EDIVE.Audio.Playback
{
    [RequireComponent(typeof(AudioSource))]
    public class StreamedAudioSource : MonoBehaviour
    {
        [Tooltip("Desired steady-state playback latency (seconds).")]
        [SerializeField] private float targetLatency = 0.25f;
        public float TargetLatency { get => targetLatency; set => targetLatency = value; }

        [Tooltip("If no new frame arrives for longer than this, stop playback (seconds).")]
        [Range(0.1f, 0.75f)]
        [SerializeField] private float frameLifetime = 0.5f;
        public float FrameLifetime { get => frameLifetime; set => frameLifetime = value; }

        [Tooltip("How large the internal ring buffer is, relative to (targetLatency + frameLifetime).")]
        [SerializeField] private int bufferFactor = 4;
        public int BufferFactor { get => bufferFactor; set => bufferFactor = value; }

        [Header("Pitch controller")]
        [Tooltip("P gain: pitch response per second of latency error.")]
        [Range(0f, 5f)]
        [SerializeField] private float pitchProportionalGain = 1f;
        public float PitchProportionalGain { get => pitchProportionalGain; set => pitchProportionalGain = value; }

        [Tooltip("Maximum absolute pitch deviation.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float pitchMaxCorrection = 0.2f;
        public float PitchMaxCorrection { get => pitchMaxCorrection; set => pitchMaxCorrection = value; }

        [Tooltip("Scale for downward (pitch < 1) correction.")]
        [Range(0f, 1.0f)]
        [SerializeField] private float downwardPitchCorrectionScale = 0.25f;
        public float DownwardPitchCorrectionScale { get => downwardPitchCorrectionScale; set => downwardPitchCorrectionScale = value; }

        [Tooltip("No pitch adjustment if |error| <= deadzone (seconds).")]
        [Range(0f, 0.05f)]
        [SerializeField] private float pitchDeadzoneSec = 0.025f;
        public float PitchDeadZoneSec { get => pitchDeadzoneSec; set => pitchDeadzoneSec = value; }

        [Tooltip("How fast pitch drifts back to 1.0 when within the deadzone.")]
        [Range(0f, 2f)]
        [SerializeField] private float pitchReturnSpeed = 0.5f;
        public float PitchReturnSpeed { get => pitchReturnSpeed; set => pitchReturnSpeed = value; }

        [Header("Startup")]
        [Tooltip("Extra safety buffer on first start (seconds). Prevents razor-edge starts.")]
        [Range(0f, 0.1f)]
        [SerializeField] private float startSafetyMarginSec = 0.02f;
        public float StartSafetyMarginSec { get => startSafetyMarginSec; set => startSafetyMarginSec = value; }

        public AudioSource UnityAudioSource
        {
            get
            {
                if (source == null)
                {
                    source = GetComponent<AudioSource>();
                    source.loop = true;
                    source.playOnAwake = false;
                    source.dopplerLevel = 0f;
                }
                return source;
            }
        }

        public int BufferDurationMS => clip != null ? clip.samples * 1000 / clip.frequency : 0;
        public int SamplingFrequency => clip != null ? clip.frequency : 0;
        public int ChannelCount => clip != null ? clip.channels : 0;
        public bool IsPlaying => UnityAudioSource.isPlaying;
        public bool IsBuffering { get; private set; }

        #region INTERNAL STATE
        private AudioSource source;
        private AudioClip clip;

        // Current format
        private int curFrequency;
        private int curChannels;

        // Ring buffer geometry (PER-CHANNEL samples)
        private int samplesPerChannel;
        private float clipLengthSec;

        // Write pointers/counters
        private int writePosPerChannel;
        private long absWritePerChannel;

        // Read tracking (absolute, unwrapped, PER-CHANNEL samples).
        // Derived from AudioSource.timeSamples + loop detection so we can compute a
        // SIGNED latency and reliably detect underruns/overruns.
        private long absReadPerChannel;
        private int lastTimeSamples;

        // Frame geometry (computed at each Feed)
        private int perChannelSamplesInFrame;
        private float secondsPerFrame;

        // Frame staleness tracking
        private readonly Stopwatch frameStopwatch = new Stopwatch();
        private float TimeSinceLastFrame => (float) frameStopwatch.Elapsed.TotalSeconds;

        private static readonly object audioWriteLock = new object();
        #endregion

        public static StreamedAudioSource New(string name = null)
        {
            var go = new GameObject(name ?? "StreamedAudioSource");
            go.hideFlags = HideFlags.DontSave;
            DontDestroyOnLoad(go);
            return go.AddComponent<StreamedAudioSource>();
        }

        /// <summary>
        /// Feed one frame of interleaved float PCM.
        /// </summary>
        public void Feed(int frequency, int channels, float[] samples)
        {
            if (!gameObject.activeInHierarchy) return;
            if (!UnityAudioSource.enabled) return;
            if (samples == null || samples.Length == 0) return;
            if (channels <= 0) channels = 1;

            // Frame geometry (channel-aware)
            perChannelSamplesInFrame = samples.Length / channels;
            secondsPerFrame = (float) perChannelSamplesInFrame / frequency;

            // Desired clip size (per-channel samples)
            int desiredClipSamplesPerCh = Mathf.CeilToInt(
                (targetLatency + frameLifetime) * bufferFactor * frequency
            );

            bool formatChanged = clip == null
                || frequency != curFrequency
                || channels != curChannels
                || desiredClipSamplesPerCh != samplesPerChannel;

            if (formatChanged)
            {
                StopPlayback();
                ReinitClip(desiredClipSamplesPerCh, channels, frequency);
            }

            // Write the frame into the ring, splitting at the wrap boundary if needed.
            lock (audioWriteLock)
            {
                WriteFrameToRing(samples);
            }

            // Advance write pointers
            absWritePerChannel += perChannelSamplesInFrame;
            writePosPerChannel = (writePosPerChannel + perChannelSamplesInFrame) % samplesPerChannel;
            frameStopwatch.Restart();

            // Startup: begin only when enough audio is prebuffered
            if (!IsPlaying)
            {
                IsBuffering = true;

                float writtenSec = (float) absWritePerChannel / curFrequency;
                if (writtenSec >= targetLatency + startSafetyMarginSec)
                {
                    // Start EXACTLY at target latency behind the write head
                    float writeTimeSec = WrappedWriteTimeSec();
                    float desiredReadSec = writeTimeSec - targetLatency;
                    if (desiredReadSec < 0f) desiredReadSec += clipLengthSec;

                    UnityAudioSource.time = desiredReadSec;
                    UnityAudioSource.pitch = 1f;
                    UnityAudioSource.Play();

                    // Seed absolute read tracking so SignedLatency == targetLatency at start.
                    absReadPerChannel = absWritePerChannel - (long) (targetLatency * curFrequency);
                    lastTimeSamples = UnityAudioSource.timeSamples;

                    IsBuffering = false;
                }
            }
        }

        private void Update()
        {
            if (!IsPlaying || clip == null) return;

            // Stale frame protection
            if (TimeSinceLastFrame > frameLifetime)
            {
                StopPlayback();
                return;
            }

            // Advance absolute read position from the looping playhead.
            int ts = UnityAudioSource.timeSamples;
            int delta = ts - lastTimeSamples;
            if (delta < 0) delta += samplesPerChannel; // playhead wrapped around the loop
            absReadPerChannel += delta;
            lastTimeSamples = ts;

            // SIGNED latency (seconds): write head minus read head, NOT wrapped.
            float latencySec = (float) (absWritePerChannel - absReadPerChannel) / curFrequency;

            // Underrun: read has caught up to / passed the write head. Stop & rebuffer.
            // Because latency is signed this is now reliable (no wrap masquerading as huge latency).
            if (latencySec < 0.5f * secondsPerFrame)
            {
                StopPlayback();
                return;
            }

            // Overrun protection: if we've drifted close to lapping the ring (write about to
            // overwrite unplayed data), hard-resync the read head back to the target instead
            // of letting the controller chase it forever.
            if (latencySec > clipLengthSec - frameLifetime)
            {
                ResyncReadToTarget();
                return;
            }

            // Proportional-only controller.
            // error > 0 -> SHORT of target -> SLOW playback -> pitch < 1
            float errorSec = targetLatency - latencySec;

            if (Mathf.Abs(errorSec) <= pitchDeadzoneSec)
            {
                UnityAudioSource.pitch = Mathf.MoveTowards(
                    UnityAudioSource.pitch, 1f, pitchReturnSpeed * UnityEngine.Time.deltaTime
                );
            }
            else
            {
                float raw = -errorSec * pitchProportionalGain;
                float minResp = -pitchMaxCorrection * downwardPitchCorrectionScale;
                float maxResp = pitchMaxCorrection;
                float resp = Mathf.Clamp(raw, minResp, maxResp);
                UnityAudioSource.pitch = 1f + resp;
            }
        }

        #region HELPERS
        private void WriteFrameToRing(float[] samples)
        {
            int roomPerCh = samplesPerChannel - writePosPerChannel;
            if (perChannelSamplesInFrame <= roomPerCh)
            {
                clip.SetData(samples, writePosPerChannel);
                return;
            }

            // Frame straddles the ring boundary: write [0, room) at the end and the rest at 0.
            int headLen = roomPerCh * curChannels;
            var head = new float[headLen];
            Array.Copy(samples, 0, head, 0, headLen);
            clip.SetData(head, writePosPerChannel);

            int tailLen = samples.Length - headLen;
            var tail = new float[tailLen];
            Array.Copy(samples, headLen, tail, 0, tailLen);
            clip.SetData(tail, 0);
        }

        private float WrappedWriteTimeSec()
        {
            int writePosWrapped = (int) (absWritePerChannel % samplesPerChannel);
            return (float) writePosWrapped / curFrequency;
        }

        private void ResyncReadToTarget()
        {
            float writeTimeSec = WrappedWriteTimeSec();
            float desiredReadSec = writeTimeSec - targetLatency;
            if (desiredReadSec < 0f) desiredReadSec += clipLengthSec;

            UnityAudioSource.time = desiredReadSec;
            UnityAudioSource.pitch = 1f;
            absReadPerChannel = absWritePerChannel - (long) (targetLatency * curFrequency);
            lastTimeSamples = UnityAudioSource.timeSamples;
        }

        private void StopPlayback()
        {
            IsBuffering = false;
            writePosPerChannel = 0;
            absWritePerChannel = 0;
            absReadPerChannel = 0;
            lastTimeSamples = 0;
            UnityAudioSource.pitch = 1f;
            UnityAudioSource.Stop();
            frameStopwatch.Reset();
        }

        private void ReinitClip(int sampleLenPerCh, int channels, int frequency)
        {
            DestroyClip();
            CreateClip(sampleLenPerCh, channels, frequency);
        }

        private void DestroyClip()
        {
            if (clip != null) Destroy(clip);
            clip = null;
        }

        private void CreateClip(int sampleLenPerCh, int channels, int frequency)
        {
            clip = AudioClip.Create("StreamedClip", sampleLenPerCh, channels, frequency, false);

            var zeros = new float[sampleLenPerCh * channels];
            clip.SetData(zeros, 0);

            UnityAudioSource.clip = clip;

            samplesPerChannel = sampleLenPerCh;
            clipLengthSec = (float) samplesPerChannel / frequency;

            curFrequency = frequency;
            curChannels = channels;

            writePosPerChannel = 0;
            absWritePerChannel = 0;
            absReadPerChannel = 0;
            lastTimeSamples = 0;
        }
        #endregion
    }
}
