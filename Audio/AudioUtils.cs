// Author: František Holubec
// Created: 23.04.2025

using System;
using System.Collections.Generic;
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

            // Calculate expected frame duration in ms
            var floatsPerFrame = firstFrame.samples.Length / sizeof(float);
            var samplesPerChannelPerFrame = floatsPerFrame / channels;
            var frameDurationMs = (double)samplesPerChannelPerFrame / frequency * 1000.0;
            
            // Threshold: if gap is more than 1.5x frame duration, it's a real gap (VAD silence)
            var gapThresholdMs = frameDurationMs * 1.5;

            // First pass: calculate total samples needed (including real gaps)
            var totalFloatSamples = 0;
            long lastFrameEndTimestamp = audioFrames[0].timestamp;
            
            foreach (var frame in audioFrames)
            {
                var gapMs = frame.timestamp - lastFrameEndTimestamp;
                
                // If there's a significant gap, add silence for it
                if (gapMs > gapThresholdMs)
                {
                    var gapSamples = (int)((gapMs / 1000.0) * frequency) * channels;
                    totalFloatSamples += gapSamples;
                }
                
                // Add this frame's samples
                var frameFloatCount = frame.samples.Length / sizeof(float);
                totalFloatSamples += frameFloatCount;
                
                // Update end timestamp for next iteration
                lastFrameEndTimestamp = frame.timestamp + (long)frameDurationMs;
            }

            if (totalFloatSamples <= 0)
                return null;

            // Create buffer initialized with silence (zeros)
            var allSamples = new float[totalFloatSamples];
            var currentOffset = 0;
            lastFrameEndTimestamp = audioFrames[0].timestamp;

            // Second pass: copy frames sequentially, inserting gaps where needed
            foreach (var frame in audioFrames)
            {
                var gapMs = frame.timestamp - lastFrameEndTimestamp;
                
                // If there's a significant gap, skip ahead (silence is already zeros)
                if (gapMs > gapThresholdMs)
                {
                    var gapSamples = (int)((gapMs / 1000.0) * frequency) * channels;
                    currentOffset += gapSamples;
                }
                
                // Copy this frame's samples
                var frameFloats = Adrenak.UniVoice.Utils.Bytes.BytesToFloats(frame.samples);
                var copyCount = Math.Min(frameFloats.Length, totalFloatSamples - currentOffset);
                
                if (copyCount > 0)
                {
                    Array.Copy(frameFloats, 0, allSamples, currentOffset, copyCount);
                    currentOffset += copyCount;
                }
                
                // Update end timestamp for next iteration
                lastFrameEndTimestamp = frame.timestamp + (long)frameDurationMs;
            }

            // Create AudioClip
            var totalSamplesPerChannel = totalFloatSamples / channels;
            var clip = AudioClip.Create("RecordedAudio", totalSamplesPerChannel, channels, frequency, false);
            clip.SetData(allSamples, 0);

            return clip;
        }
        
        public static bool TryProcessAudioFrame(ref AudioFrame frame, List<IAudioFilter> filters)
        {
            if (filters != null) {
                foreach (var filter in filters) {
                    frame = filter.Run(frame);
                    if (frame.samples == null)
                        break;
                }
            }

            return frame.samples != null && frame.samples.Length > 0;
        }
    }
}
