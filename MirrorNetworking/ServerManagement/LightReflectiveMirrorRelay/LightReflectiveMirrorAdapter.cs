// Author: František Holubec
// Created: 15.07.2025

using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using LightReflectiveMirror;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.MirrorNetworking.ServerManagement.LightReflectiveMirrorRelay
{
    public class LightReflectiveMirrorAdapter : AServerListAdapter
    {
        [SerializeField]
        private LightReflectiveMirrorConfig _RelayConfig;

        [SerializeField]
        private LightReflectiveMirrorTransport _Transport;

        [SerializeField]
        private float _ConnectTimeout = 5f;

        public override UniTask Initialize(ServerConfig serverConfig)
        {
            _RelayConfig.ApplyTo(_Transport);
            _Transport.ConnectToRelay();
            
            var extraData = new ExtraServerData
            {
                ServerID = serverConfig.ServerID
            };
            
            _Transport.extraServerData = JsonConvert.SerializeObject(extraData);
            _Transport.serverName = serverConfig.ServerName;
            _Transport.maxServerPlayers = serverConfig.MaxPlayers;
            _Transport.serverListUpdated.AddListener(OnServerListUpdated);

            AwaitRelay().Forget();
            return UniTask.CompletedTask;
        }
 
        private async UniTask AwaitRelay()
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfterSlim(TimeSpan.FromSeconds(_ConnectTimeout));
            try
            {
                await UniTask.WaitUntil(_Transport.IsAuthenticated, PlayerLoopTiming.Update, cts.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.LogError("LRM not available");
            }
        }

        private void OnServerListUpdated()
        {
            Servers.Clear();
            AddServers(_Transport.relayServerList.Select(GetRecord));
        }

        public override void StartSearch()
        {
            _Transport.RequestServerList();
        }

        public override void StopSearch() { }

        private static ServerRecord GetRecord(Room room)
        {
            var extraData = JsonConvert.DeserializeObject<ExtraServerData>(room.serverData);
            return new ServerRecord()
            {
                Address = room.serverId,
                ServerID = extraData.ServerID,
                ServerName = room.serverName,
                MaxPlayers = room.maxPlayers,
                CurrentPlayers = room.currentPlayers
            };
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class ExtraServerData
        {
            [JsonProperty]
            public long ServerID;
        }
    }
}
