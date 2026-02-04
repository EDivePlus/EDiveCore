// Author: František Holubec
// Created: 05.02.2026

using Adrenak.UniMic;
using Adrenak.UniVoice;
using UnityEngine;

namespace EDIVE.Audio
{
    [RequireComponent(typeof(StreamedAudioSource))]
    public class EnhancedStreamedAudioSourceOutput : MonoBehaviour, IAudioOutput
    {
        private const string TAG = "[StreamedAudioSourceOutput]";

        public StreamedAudioSource Stream { get; private set; }
        
        public event System.Action<AudioFrame> AudioFrameReceived;

        [System.Obsolete("Cannot use new keyword to create an instance. Use the .New() method instead")]
        public EnhancedStreamedAudioSourceOutput() { }

        /// <summary>
        /// Creates a new instance using the dependencies.
        /// </summary>
        public static EnhancedStreamedAudioSourceOutput New()
        {
            var go = new GameObject("StreamedAudioSourceOutput");
            DontDestroyOnLoad(go);
            var output = go.AddComponent<EnhancedStreamedAudioSourceOutput>();
            output.Stream = go.GetComponent<StreamedAudioSource>();
            Debug.unityLogger.Log(LogType.Log, TAG, "StreamedAudioSource created");
            return output;
        }

        /// <summary>
        /// Feeds an incoming <see cref="ChatroomAudioSegment"/> into the audio buffer.
        /// </summary>
        /// <param name="frame"></param>
        public void Feed(AudioFrame frame)
        {
            Stream.Feed(frame.frequency, frame.channelCount, Adrenak.UniVoice.Utils.Bytes.BytesToFloats(frame.samples));
            AudioFrameReceived?.Invoke(frame);
        }

        /// <summary>
        /// Disposes the instance by deleting the GameObject of the component.
        /// </summary>
        public void Dispose()
        {
            Debug.unityLogger.Log(LogType.Log, TAG, "Disposing StreamedAudioSource");
            Destroy(gameObject);
        }
    }
}
