// Author: František Holubec
// Created: 23.04.2025

using System;
using System.Collections.Generic;
using System.Linq;
using Adrenak.UniVoice;
using UnityEngine;
using ZLinq;

namespace EDIVE.Audio
{
    public static class AudioUtils
    {
        public static void CheckMicrophonePermission(Action<bool> callback)
        {
#if UNITY_ANDROID
            if (UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
            {
                callback?.Invoke(true);
                return;
            }

            var callbacks = new UnityEngine.Android.PermissionCallbacks();
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone, callbacks);
            callbacks.PermissionGranted += _ => callback?.Invoke(true);
            callbacks.PermissionDenied += _ => callback?.Invoke(false);
#else
            callback?.Invoke(true);
#endif
        }

        public static AudioClip CreateAudioClip(List<AudioFrame> audioFrames)
        {
            if (audioFrames == null || audioFrames.Count == 0)
                return null;

            // Get format from first frame
            var firstFrame = audioFrames[0];
            var frequency = firstFrame.frequency;
            var channels = firstFrame.channelCount;

            // Check all frames have the same format
            if (audioFrames.AsValueEnumerable().Any(frame => frame.frequency != frequency || frame.channelCount != channels))
            {
                Debug.LogError("Cannot create AudioClip from audio frames with different formats.");
                return null;
            }

            // Sort frames by timestamp
            audioFrames.Sort((a, b) => a.timestamp.CompareTo(b.timestamp));

            // Calculate frame duration in ms based on first frame
            var samplesPerFrame = firstFrame.samples.Length / sizeof(float);
            var frameDurationMs = (float)samplesPerFrame / channels / frequency * 1000f;

            // Calculate total duration based on timestamps
            var startTimestamp = audioFrames[0].timestamp;
            var endTimestamp = audioFrames[^1].timestamp;
            var totalDurationMs = endTimestamp - startTimestamp + (long)frameDurationMs;

            // Calculate total samples needed (including gaps)
            var totalSamplesPerChannel = (int)(totalDurationMs / 1000f * frequency);
            if (totalSamplesPerChannel <= 0)
                return null;
            var totalSamples = totalSamplesPerChannel * channels;

            // Create buffer initialized with silence (zeros)
            var allSamples = new float[totalSamples];

            // Place each frame at its correct position based on timestamp
            foreach (var frame in audioFrames.AsValueEnumerable())
            {
                // Calculate offset in samples from start
                var offsetMs = frame.timestamp - startTimestamp;
                var offsetSamples = (int)(offsetMs / 1000f * frequency * channels);
                if (offsetSamples < 0 || offsetSamples >= totalSamples)
                    continue;

                // Copy raw bytes directly into the float buffer to avoid per-frame allocations
                var destByteOffset = offsetSamples * sizeof(float);
                var availableBytes = (totalSamples - offsetSamples) * sizeof(float);
                var copyBytes = Math.Min(frame.samples.Length, availableBytes);
                if (copyBytes > 0)
                    Buffer.BlockCopy(frame.samples, 0, allSamples, destByteOffset, copyBytes);
            }

            // Create AudioClip
            var clip = AudioClip.Create("RecordedAudio", totalSamplesPerChannel, channels, frequency, false);
            clip.SetData(allSamples, 0);

            return clip;
        }
    }
}
