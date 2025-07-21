// Author: František Holubec
// Created: 07.07.2025

#if R3
using System;
using MemoryPack;
using Newtonsoft.Json;
using R3;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Utils.R3
{
    [Serializable]
    [InlineProperty]
    [MemoryPackable]
    [JsonObject(MemberSerialization.OptIn)]
    public partial class TimeProviderPreset
    {
        [HideLabel]
        [HorizontalGroup]
        [SerializeField]
        [MemoryPackInclude]
        [JsonProperty("TimeKind")]
        private TimeKind _TimeKind = TimeKind.Time;
        
        [HideLabel]
        [HorizontalGroup]
        [SerializeField]
        [MemoryPackInclude]
        [JsonProperty("LoopTiming")]
        private PlayerLoopTiming _LoopTiming = PlayerLoopTiming.Update;

        [MemoryPackIgnore]
        public TimeKind TimeKind => _TimeKind;
        
        [MemoryPackIgnore]
        public PlayerLoopTiming LoopTiming => _LoopTiming;

        [MemoryPackConstructor]
        public TimeProviderPreset() { }
        public TimeProviderPreset(PlayerLoopTiming loopTiming, TimeKind timeKind = TimeKind.Time)
        {
            _LoopTiming = loopTiming;
            _TimeKind = timeKind;
        }

        public static implicit operator TimeProvider(TimeProviderPreset preset) => preset.GetProvider();

        public TimeProvider GetProvider() => _LoopTiming switch
        {
            PlayerLoopTiming.Initialization => _TimeKind switch
            {
                TimeKind.Time => UnityTimeProvider.Initialization,
                TimeKind.UnscaledTime => UnityTimeProvider.InitializationIgnoreTimeScale,
                TimeKind.Realtime => UnityTimeProvider.InitializationRealtime,
                _ => throw new ArgumentOutOfRangeException()
            },

            PlayerLoopTiming.EarlyUpdate => _TimeKind switch
            {
                TimeKind.Time => UnityTimeProvider.EarlyUpdate,
                TimeKind.UnscaledTime => UnityTimeProvider.EarlyUpdateIgnoreTimeScale,
                TimeKind.Realtime => UnityTimeProvider.EarlyUpdateRealtime,
                _ => throw new ArgumentOutOfRangeException()
            },
            PlayerLoopTiming.FixedUpdate => _TimeKind switch
            {
                TimeKind.Time => UnityTimeProvider.FixedUpdate,
                TimeKind.UnscaledTime => UnityTimeProvider.FixedUpdateIgnoreTimeScale,
                TimeKind.Realtime => UnityTimeProvider.FixedUpdateRealtime,
                _ => throw new ArgumentOutOfRangeException()
            },
            PlayerLoopTiming.PreUpdate => _TimeKind switch
            {
                TimeKind.Time => UnityTimeProvider.PreUpdate,
                TimeKind.UnscaledTime => UnityTimeProvider.PreUpdateIgnoreTimeScale,
                TimeKind.Realtime => UnityTimeProvider.PreUpdateRealtime,
                _ => throw new ArgumentOutOfRangeException()
            },
            PlayerLoopTiming.Update => _TimeKind switch
            {
                TimeKind.Time => UnityTimeProvider.Update,
                TimeKind.UnscaledTime => UnityTimeProvider.UpdateIgnoreTimeScale,
                TimeKind.Realtime => UnityTimeProvider.UpdateRealtime,
                _ => throw new ArgumentOutOfRangeException()
            },
            PlayerLoopTiming.PreLateUpdate => _TimeKind switch
            {
                TimeKind.Time => UnityTimeProvider.PreLateUpdate,
                TimeKind.UnscaledTime => UnityTimeProvider.PreLateUpdateIgnoreTimeScale,
                TimeKind.Realtime => UnityTimeProvider.PreLateUpdateRealtime,
                _ => throw new ArgumentOutOfRangeException()
            },
            PlayerLoopTiming.PostLateUpdate => _TimeKind switch
            {
                TimeKind.Time => UnityTimeProvider.PostLateUpdate,
                TimeKind.UnscaledTime => UnityTimeProvider.PostLateUpdateIgnoreTimeScale,
                TimeKind.Realtime => UnityTimeProvider.PostLateUpdateRealtime,
                _ => throw new ArgumentOutOfRangeException()
            },
            PlayerLoopTiming.TimeUpdate => _TimeKind switch
            {
                TimeKind.Time => UnityTimeProvider.TimeUpdate,
                TimeKind.UnscaledTime => UnityTimeProvider.TimeUpdateIgnoreTimeScale,
                TimeKind.Realtime => UnityTimeProvider.TimeUpdateRealtime,
                _ => throw new ArgumentOutOfRangeException()
            },
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
#endif
