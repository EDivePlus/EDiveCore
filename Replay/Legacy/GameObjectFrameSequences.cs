// Author: František Holubec
// Created: 04.07.2025

using System;
using EDIVE.Replay.Components;
using EDIVE.Utils.Cysharp;
using MemoryPack;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.Replay.Frames
{
    [Serializable]
    [MemoryPackable, MemoryPackUnionTag(2)]
    [JsonObject(MemberSerialization.OptIn)]
    public partial class GameObjectActiveFrameSequence : AFrameSequence<GameObject, GameObjectActiveComponent.FramePreset>
    {
        public override AReplayAgentComponent Migrate(ReplayAgentComponent component)
        {
            return new GameObjectActiveComponent(component.Target as GameObject, new GameObjectActiveComponent.ComponentData(component.ID, _Frames));
        }
    }
}
