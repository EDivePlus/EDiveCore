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

        private int curFrequency;
        private int curChannels;

        // Ring buffer geometry (per-channel samples)
        private int samplesPerChannel;
        private float clipLengthSec;

        private int writePosPerChannel;
        private long absWritePerChannel;

        // Absolute unwrapped read position, used for signed latency
        private long absReadPerChannel;
        private int lastTimeSamples;

        private int perChannelSamplesInFrame;
        private float secondsPerFrame;

        // Zeros kept ahead of the write head so the playhead reads silence, not stale data, on overrun
        private float[] silenceGuard;
        private int silenceGuardPerChannel;

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

        // Feed one frame of interleaved float PCM.
        public void Feed(int frequency, int channels, float[] samples)
        {
            if (!gameObject.activeInHierarchy) return;
            if (!UnityAudioSource.enabled) return;
            if (samples == null || samples.Length == 0) return;
            if (channels <= 0) channels = 1;

            perChannelSamplesInFrame = samples.Length / channels;
            secondsPerFrame = (float) perChannelSamplesInFrame / frequency;

            var desiredClipSamplesPerCh = Mathf.CeilToInt(
                (targetLatency + frameLifetime) * bufferFactor * frequency
            );

            var formatChanged = clip == null
                                || frequency != curFrequency
                                || channels != curChannels
                                || desiredClipSamplesPerCh != samplesPerChannel;

            if (formatChanged)
            {
                StopPlayback();
                ReinitClip(desiredClipSamplesPerCh, channels, frequency);
            }

            // Write frame, advance write head, refresh silence guard, all under one lock
            lock (audioWriteLock)
            {
                WriteFrameToRing(samples);

                absWritePerChannel += perChannelSamplesInFrame;
                writePosPerChannel = (writePosPerChannel + perChannelSamplesInFrame) % samplesPerChannel;

                WriteSilenceGuard();
            }
            frameStopwatch.Restart();

            // Start only once enough audio is prebuffered
            if (!IsPlaying)
            {
                IsBuffering = true;

                var writtenSec = (float) absWritePerChannel / curFrequency;
                if (writtenSec >= targetLatency + startSafetyMarginSec)
                {
                    // Start exactly targetLatency behind the write head
                    var writeTimeSec = WrappedWriteTimeSec();
                    var desiredReadSec = writeTimeSec - targetLatency;
                    if (desiredReadSec < 0f) desiredReadSec += clipLengthSec;

                    UnityAudioSource.time = desiredReadSec;
                    UnityAudioSource.pitch = 1f;
                    UnityAudioSource.Play();

                    absReadPerChannel = absWritePerChannel - (long) (targetLatency * curFrequency);
                    lastTimeSamples = UnityAudioSource.timeSamples;

                    IsBuffering = false;
                }
            }
        }

        private void Update()
        {
            if (!IsPlaying || clip == null) return;

            // Stop if no frame has arrived recently
            if (TimeSinceLastFrame > frameLifetime)
            {
                StopPlayback();
                return;
            }

            // Advance absolute read position from the looping playhead
            var ts = UnityAudioSource.timeSamples;
            var delta = ts - lastTimeSamples;
            if (delta < 0) delta += samplesPerChannel; // playhead wrapped around the loop
            absReadPerChannel += delta;
            lastTimeSamples = ts;

            // Signed latency: write head minus read head
            var latencySec = (float) (absWritePerChannel - absReadPerChannel) / curFrequency;

            // Underrun: read caught up to the write head
            if (latencySec < 0.5f * secondsPerFrame)
            {
                StopPlayback();
                return;
            }

            // Overrun: about to lap the ring, resync instead of chasing
            if (latencySec > clipLengthSec - frameLifetime)
            {
                ResyncReadToTarget();
                return;
            }

            // error > 0 -> short of target -> slow playback -> pitch < 1
            var errorSec = targetLatency - latencySec;

            if (Mathf.Abs(errorSec) <= pitchDeadzoneSec)
            {
                UnityAudioSource.pitch = Mathf.MoveTowards(
                    UnityAudioSource.pitch, 1f, pitchReturnSpeed * UnityEngine.Time.deltaTime
                );
            }
            else
            {
                var raw = -errorSec * pitchProportionalGain;
                var minResp = -pitchMaxCorrection * downwardPitchCorrectionScale;
                var maxResp = pitchMaxCorrection;
                var resp = Mathf.Clamp(raw, minResp, maxResp);
                UnityAudioSource.pitch = 1f + resp;
            }
        }

        #region HELPERS
        private void WriteFrameToRing(float[] samples)
        {
            var roomPerCh = samplesPerChannel - writePosPerChannel;
            if (perChannelSamplesInFrame <= roomPerCh)
            {
                clip.SetData(samples, writePosPerChannel);
                return;
            }

            // Frame straddles the ring boundary: split across end and start
            var headLen = roomPerCh * curChannels;
            var head = new float[headLen];
            Array.Copy(samples, 0, head, 0, headLen);
            clip.SetData(head, writePosPerChannel);

            var tailLen = samples.Length - headLen;
            var tail = new float[tailLen];
            Array.Copy(samples, headLen, tail, 0, tailLen);
            clip.SetData(tail, 0);
        }

        // Writes zeros ahead of the write head so the playhead reads silence, not stale PCM, on underrun
        private void WriteSilenceGuard()
        {
            if (silenceGuard == null || silenceGuardPerChannel <= 0) return;

            var roomPerCh = samplesPerChannel - writePosPerChannel;
            if (silenceGuardPerChannel <= roomPerCh)
            {
                clip.SetData(silenceGuard, writePosPerChannel);
                return;
            }

            // Guard straddles the ring boundary: split across end and start
            var headLen = roomPerCh * curChannels;
            clip.SetData(new float[headLen], writePosPerChannel);

            var tailLen = silenceGuard.Length - headLen;
            clip.SetData(new float[tailLen], 0);
        }

        private float WrappedWriteTimeSec()
        {
            var writePosWrapped = (int) (absWritePerChannel % samplesPerChannel);
            return (float) writePosWrapped / curFrequency;
        }

        private void ResyncReadToTarget()
        {
            var writeTimeSec = WrappedWriteTimeSec();
            var desiredReadSec = writeTimeSec - targetLatency;
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

            // Keep the guard shorter than the ring so it never wraps onto unplayed data
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
