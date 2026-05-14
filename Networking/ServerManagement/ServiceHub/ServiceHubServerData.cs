// Author: Michal Petr
// Created: 12.05.2026

using Newtonsoft.Json;

namespace EDIVE.Networking.ServerManagement.ServiceHub
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ServiceHubServerData
    {
        [JsonProperty("instance_id")]
        public string InstanceID;
        
        [JsonProperty("unity_relay_code")]
        public string UnityRelayCode;
        
        [JsonProperty("purr_relay_room")]
        public string PurrRelayRoom;

        public string Serialize() => JsonConvert.SerializeObject(this);

        public static ServiceHubServerData TryParse(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return null;
            try { return JsonConvert.DeserializeObject<ServiceHubServerData>(raw); }
            catch { return null; }
        }
    }
}
