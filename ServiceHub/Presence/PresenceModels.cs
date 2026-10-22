// Author: František Holubec
// Created: 20.10.2026

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EDIVE.ServiceHub.Presence
{
    public static class PresenceDevice
    {
        public const string HEADSET = "headset";
        public const string DESKTOP = "desktop";
        public const string MOBILE = "mobile";
    }

    public static class DisplayAllow
    {
        public const string ANYONE = "anyone";
        public const string MANAGERS = "managers";
        public const string NOBODY = "nobody";
    }

    public static class PresenceResult
    {
        public const string APPLIED = "applied";
        public const string FAILED = "failed";
        public const string UNSUPPORTED = "unsupported";
        public const string ALLOW = "allow";
        public const string DENY = "deny";
        public const string KEEP = "keep";
        public const string HANDOVER = "handover";
        public const string TIMEOUT = "timeout";
        public const string REFUSED = "refused";
    }

    public static class PresenceCommandType
    {
        public const string DISPLAY_SHOW = "display.show";
        public const string DISPLAY_CLEAR = "display.clear";
        public const string DISPLAY_HANDOVER = "display.handover";
        public const string SHARE_REQUEST = "share.request";
        public const string SHARE_ALLOW = "share.allow";
        public const string SHARE_STOP = "share.stop";
        public const string SHARE_VIEWERS = "share.viewers";
        public const string NOTICE = "notice";
    }

    // --------
    // Requests
    // --------

    [JsonObject(MemberSerialization.OptIn)]
    public class PresenceRequest
    {
        [JsonProperty("protocol")]
        public int Protocol;

        [JsonProperty("instance_id", NullValueHandling = NullValueHandling.Ignore)]
        public string InstanceId;

        [JsonProperty("join_code", NullValueHandling = NullValueHandling.Ignore)]
        public string JoinCode;

        [JsonProperty("device", NullValueHandling = NullValueHandling.Ignore)]
        public string Device;

        [JsonProperty("displays")]
        public List<PresenceDisplay> Displays;

        [JsonProperty("sources")]
        public List<PresenceSource> Sources;

        // Source IDs this client receives now
        [JsonProperty("watching")]
        public List<string> Watching;
    }

    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class PresenceDisplay
    {
        [JsonProperty("id")]
        public string Id;

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("place", NullValueHandling = NullValueHandling.Ignore)]
        public string Place;

        [JsonProperty("allow")]
        public string Allow = DisplayAllow.ANYONE;

        [JsonProperty("source")]
        public string Source;

        [JsonProperty("presenter")]
        public string Presenter;
    }

    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class PresenceSource
    {
        // view:<participant>, present:<participant>, also the LiveKit track name
        [JsonProperty("id")]
        public string Id;

        [JsonProperty("label")]
        public string Label;

        [JsonProperty("has_audio")]
        public bool HasAudio;

        // False for a share started by a request, then only Allowed may watch
        [JsonProperty("open")]
        public bool Open = true;

        // Participant identities
        [JsonProperty("allowed", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Allowed;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class PresenceReply
    {
        [JsonProperty("result")]
        public string Result;

        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Data;

        public PresenceReply(string result, JObject data = null)
        {
            Result = result;
            Data = data;
        }
    }

    // ---------
    // Responses
    // ---------

    [JsonObject(MemberSerialization.OptIn)]
    public class PresenceResponse
    {
        [JsonProperty("identity")]
        public string Identity;

        // Identity in LiveKit, used in source IDs
        [JsonProperty("participant")]
        public string Participant;

        // Null when outside a session or the code didn't match
        [JsonProperty("join_code")]
        public string JoinCode;

        [JsonProperty("expires_in")]
        public int ExpiresIn;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class PresenceCommand
    {
        [JsonProperty("seq")]
        public long Seq;

        [JsonProperty("id")]
        public string Id;

        [JsonProperty("type")]
        public string Type;

        [JsonProperty("data")]
        public JObject Data;

        [JsonProperty("expires_at")]
        public DateTime ExpiresAt;

        public string GetString(string key) => Data?.Value<string>(key);
        public TimeSpan Remaining => ExpiresAt.ToUniversalTime() - DateTime.UtcNow;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class PresenceCommandsResponse
    {
        [JsonProperty("commands")]
        public List<PresenceCommand> Commands;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class PresenceEmptyResponse { }

    [JsonObject(MemberSerialization.OptIn)]
    public class StreamTokenRequest
    {
        // Left out for the personal room outside a session
        [JsonProperty("join_code", NullValueHandling = NullValueHandling.Ignore)]
        public string JoinCode;

        [JsonProperty("instance_id")]
        public string InstanceId;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class StreamTokenResponse
    {
        [JsonProperty("url")]
        public string Url;

        [JsonProperty("token")]
        public string Token;

        [JsonProperty("room")]
        public string Room;

        // Participant identity in LiveKit
        [JsonProperty("identity")]
        public string Identity;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class RequestViewRequest
    {
        // User or guest ID of someone in the same session
        [JsonProperty("identity")]
        public string Identity;

        [JsonProperty("instance_id")]
        public string InstanceId;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class DisplayRequest
    {
        [JsonProperty("display_id")]
        public string DisplayId;

        // Null turns the display off
        [JsonProperty("source", NullValueHandling = NullValueHandling.Include)]
        public string Source;

        [JsonProperty("instance_id")]
        public string InstanceId;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class LiveSourcesResponse
    {
        [JsonProperty("sources")]
        public List<LiveSource> Sources;
    }

    // Something live in the session, from ServiceHub
    [JsonObject(MemberSerialization.OptIn)]
    public class LiveSource
    {
        [JsonProperty("source")]
        public string Source;

        // view or present
        [JsonProperty("kind")]
        public string Kind;

        [JsonProperty("participant")]
        public string Participant;

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("web")]
        public bool Web;

        [JsonProperty("has_audio")]
        public bool HasAudio;

        [JsonProperty("open")]
        public bool Open;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class StreamActionResponse
    {
        [JsonProperty("result")]
        public string Result;

        [JsonProperty("message")]
        public string Message;
    }
}
