using System;
using System.Collections.Generic;
using System.Linq;
using Adrenak.BRW;
using UnityEngine;

namespace Adrenak.UniVoice.Networks
{

    public abstract class AAudioClientBase<T> : IAudioClient<T>
    {
        public T ID { get; protected set; }
        public List<T> PeerIDs { get; private set; } = new();
        public VoiceSettings YourVoiceSettings { get; private set; } = new();

        public event Action<T, List<T>> OnJoined;
        public event Action OnLeft;
        public event Action<T> OnPeerJoined;
        public event Action<T> OnPeerLeft;
        public event Action<T, AudioFrame> OnReceivedPeerAudioFrame;

        protected abstract string Tag { get; }
        protected abstract bool HasLocalId { get; }
        protected abstract bool IsConnected { get; }

        protected abstract void SendToServer(byte[] data, bool reliable);
        protected abstract void WriteId(BytesWriter writer, T id);
        protected abstract T ReadId(BytesReader reader);
        
        protected abstract void ResetLocalId();

        public virtual void Dispose()
        {
            PeerIDs.Clear();
        }

        public void SendAudioFrame(AudioFrame frame)
        {
            if (!HasLocalId || !IsConnected) return;

            var writer = new BytesWriter();
            writer.WriteString(AudioBroadcastTags.AUDIO_FRAME);
            WriteId(writer, ID);
            writer.WriteLong(frame.timestamp);
            writer.WriteInt(frame.frequency);
            writer.WriteInt(frame.channelCount);
            writer.WriteByteArray(frame.samples);

            SendToServer(writer.Bytes, reliable: false);
        }

        public void SubmitVoiceSettings()
        {
            if (!HasLocalId || !IsConnected) return;
            Debug.unityLogger.Log(Tag, "Submitting : " + YourVoiceSettings);

            var writer = new BytesWriter();
            writer.WriteString(AudioBroadcastTags.VOICE_SETTINGS);
            writer.WriteInt(YourVoiceSettings.muteAll ? 1 : 0);
            writer.WriteIntArray(YourVoiceSettings.mutedPeers.ToArray());
            writer.WriteInt(YourVoiceSettings.deafenAll ? 1 : 0);
            writer.WriteIntArray(YourVoiceSettings.deafenedPeers.ToArray());
            writer.WriteStringArray(YourVoiceSettings.myTags.ToArray());
            writer.WriteStringArray(YourVoiceSettings.mutedTags.ToArray());
            writer.WriteStringArray(YourVoiceSettings.deafenedTags.ToArray());

            SendToServer(writer.Bytes, reliable: true);
        }

        public void UpdateVoiceSettings(Action<VoiceSettings> modification)
        {
            modification?.Invoke(YourVoiceSettings);
            SubmitVoiceSettings();
        }

        protected void HandleIncomingPayload(byte[] data)
        {
            var reader = new BytesReader(data);
            var tag = reader.ReadString();
            DispatchIncomingTag(tag, reader);
        }
        
        protected virtual void DispatchIncomingTag(string tag, BytesReader reader)
        {
            if (tag == AudioBroadcastTags.AUDIO_FRAME)
                ReadAudioFrame(reader);
        }

        protected void ReadAudioFrame(BytesReader reader)
        {
            var senderId = ReadId(reader);
            var frame = new AudioFrame
            {
                timestamp = reader.ReadLong(),
                frequency = reader.ReadInt(),
                channelCount = reader.ReadInt(),
                samples = reader.ReadByteArray()
            };
            if (EqualityComparer<T>.Default.Equals(senderId, ID)) return;
            if (!PeerIDs.Contains(senderId)) return;
            OnReceivedPeerAudioFrame?.Invoke(senderId, frame);
        }

        protected void HandleLocalJoined(T localId, IEnumerable<T> existingPeers)
        {
            ID = localId;
            PeerIDs = existingPeers
                .Where(p => !EqualityComparer<T>.Default.Equals(p, localId))
                .ToList();

            var log = $"Initialized with ID {ID}. ";
            log += PeerIDs.Count > 0
                ? $"Peer list: {string.Join(", ", PeerIDs)}"
                : "There are currently no peers.";
            Debug.unityLogger.Log(LogType.Log, Tag, log);

            OnJoined?.Invoke(ID, PeerIDs);
            foreach (var peerId in PeerIDs)
                OnPeerJoined?.Invoke(peerId);
        }

        protected void HandleLocalLeft()
        {
            YourVoiceSettings = new VoiceSettings();
            var oldPeerIds = PeerIDs.ToList();
            PeerIDs.Clear();
            ResetLocalId();
            foreach (var peerId in oldPeerIds)
                OnPeerLeft?.Invoke(peerId);
            OnLeft?.Invoke();
        }

        protected void HandlePeerJoined(T peerId)
        {
            if (HasLocalId && EqualityComparer<T>.Default.Equals(peerId, ID)) return;
            if (PeerIDs.Contains(peerId)) return;

            PeerIDs.Add(peerId);
            Debug.unityLogger.Log(LogType.Log, Tag,
                $"Peer {peerId} joined. Peer list is now {string.Join(", ", PeerIDs)}");
            OnPeerJoined?.Invoke(peerId);
        }

        protected void HandlePeerLeft(T peerId)
        {
            if (!PeerIDs.Remove(peerId)) return;

            var log = $"Peer {peerId} left. ";
            log += PeerIDs.Count == 0
                ? "There are no peers anymore."
                : $"Peer list is now {string.Join(", ", PeerIDs)}";
            Debug.unityLogger.Log(LogType.Log, Tag, log);
            OnPeerLeft?.Invoke(peerId);
        }
    }
}
