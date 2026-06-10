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

        [Tooltip("Length of the silence written ahead of the write head (seconds). Prevents the looping " +
                 "clip from replaying stale audio when the consumer catches up to / overruns the producer.")]
        [Range(0.02f, 0.2f)]
        [SerializeField] private float silenceGuardSec = 0.08f;
        public float SilenceGuardSec { get => silenceGuardSec; set => silenceGuardSec = value; }

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

        // Silence guard: a block of zeros kept just ahead of the write head so the looping
        // playhead reads silence (not stale data) if it ever catches up to / overruns the producer.
        private float[] silenceGuard;
        private int silenceGuardPerChannel;

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

            // Write the frame into the ring, advance the write head, then lay down a fresh
            // silence guard immediately ahead of it. All under one lock so the guard and the
            // advanced pointer are always consistent with the data actually present in the clip.
            lock (audioWriteLock)
            {
                WriteFrameToRing(samples);

                absWritePerChannel += perChannelSamplesInFrame;
                writePosPerChannel = (writePosPerChannel + perChannelSamplesInFrame) % samplesPerChannel;

                WriteSilenceGuard();
            }
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

        // Writes a block of zeros starting at the current write head, wrapping at the ring
        // boundary if needed. This region sits "in the future" relative to the read head, so
        // overwriting it is always safe; the next real frame overwrites it again. If the
        // consumer ever reaches the write head (gap / underrun / main-thread hitch), it reads
        // this silence instead of replaying stale PCM left over from the previous lap.
        private void WriteSilenceGuard()
        {
            if (silenceGuard == null || silenceGuardPerChannel <= 0) return;

            int roomPerCh = samplesPerChannel - writePosPerChannel;
            if (silenceGuardPerChannel <= roomPerCh)
            {
                clip.SetData(silenceGuard, writePosPerChannel);
                return;
            }

            // Guard straddles the ring boundary: write zeros at the end, then the rest at 0.
            int headLen = roomPerCh * curChannels;
            clip.SetData(new float[headLen], writePosPerChannel);

            int tailLen = silenceGuard.Length - headLen;
            clip.SetData(new float[tailLen], 0);
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

            // Keep the guard shorter than the ring so it can never wrap onto unplayed real data.
            silenceGuardPerChannel = Mathf.Clamp(Mathf.CeilToInt(silenceGuardSec * frequency), 1, samplesPerChannel - 1);
            silenceGuard = new float[silenceGuardPerChannel * channels];

            writePosPerChannel = 0;
            absWritePerChannel = 0;
            absReadPerChannel = 0;
            lastTimeSamples = 0;
        }
        #endregion
    }
}
