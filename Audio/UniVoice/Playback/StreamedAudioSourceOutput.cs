using Adrenak.UniVoice;
using UnityEngine;

namespace EDIVE.Audio.Playback
{
    [RequireComponent(typeof(StreamedAudioSource))]
    public class StreamedAudioSourceOutput : MonoBehaviour, IAudioOutput
    {
        private const string TAG = "[StreamedAudioSourceOutput]";

        public StreamedAudioSource Stream { get; private set; }

        public static StreamedAudioSourceOutput New()
        {
            var go = new GameObject("StreamedAudioSourceOutput");
            DontDestroyOnLoad(go);
            var created = go.AddComponent<StreamedAudioSourceOutput>();
            created.Stream = go.GetComponent<StreamedAudioSource>();
            Debug.unityLogger.Log(LogType.Log, TAG, "StreamedAudioSource created");
            return created;
        }

        public void Feed(AudioFrame frame)
        {
            Stream.Feed(frame.frequency, frame.channelCount, Adrenak.UniVoice.Utils.Bytes.BytesToFloats(frame.samples));
        }

        public void Dispose()
        {
            Debug.unityLogger.Log(LogType.Log, TAG, "Disposing StreamedAudioSource");
            if (this == null)
                return;
            Destroy(gameObject);
        }

        public class Factory : IAudioOutputFactory
        {
            public IAudioOutput Create() => New();
        }
    }
}
