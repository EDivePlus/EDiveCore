// Author: Radim Holub
// Created: 19.02.2026

using System;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.ServiceHub.SaveData
{
    [Serializable]
    public class PlayerPrefsSaveDataStore : ASaveDataStore
    {
        public override bool SupportsCheapTimestamp => true;

        public string Prefix => $"servicehub.savedata.{Context.Key}.";

        private string BuildKey(string key) => Prefix + (key ?? "");

        [Serializable]
        private class TimestampedValue
        {
            [JsonProperty("v")] public string Value;
            [JsonProperty("ts")] public long Timestamp;
        }

        public override UniTask<StoreReadResult> GetAsync(string key)
        {
            var k = BuildKey(key);
            if (!PlayerPrefs.HasKey(k))
                return UniTask.FromResult(StoreReadResult.NotFound);

            var stored = PlayerPrefs.GetString(k, "");
            if (string.IsNullOrWhiteSpace(stored))
                return UniTask.FromResult(StoreReadResult.NotFound);

            var entry = TryDeserialize(stored);
            if (entry?.Value == null)
                return UniTask.FromResult(StoreReadResult.Hit(stored, null)); // legacy raw json

            return UniTask.FromResult(StoreReadResult.Hit(entry.Value, TicksToTimestamp(entry.Timestamp)));
        }

        public override UniTask SetAsync(ASaveDataObject saveDataObject)
        {
            var k = BuildKey(saveDataObject.Key);
            var json = JsonConvert.SerializeObject(saveDataObject);
            Write(k, json, ReadTicks(k));
            return UniTask.CompletedTask;
        }

        public override UniTask SetRawAsync(string key, string json, DateTime? timestamp)
        {
            Write(BuildKey(key), json, TimestampToTicks(timestamp));
            return UniTask.CompletedTask;
        }

        public override UniTask SetTimestampAsync(string key, DateTime timestamp)
        {
            var k = BuildKey(key);
            if (!PlayerPrefs.HasKey(k))
                return UniTask.CompletedTask;

            var (found, json) = ReadValue(k);
            if (found)
                Write(k, json, TimestampToTicks(timestamp));

            return UniTask.CompletedTask;
        }

        public override UniTask DeleteAsync(string key)
        {
            PlayerPrefs.DeleteKey(BuildKey(key));
            return UniTask.CompletedTask;
        }

        public override UniTask<bool> HasKeyAsync(string key)
        {
            return UniTask.FromResult(PlayerPrefs.HasKey(BuildKey(key)));
        }

        public override UniTask FlushAsync()
        {
            PlayerPrefs.Save();
            return UniTask.CompletedTask;
        }

        private static void Write(string fullKey, string json, long ticks)
        {
            var entry = new TimestampedValue { Value = json ?? "", Timestamp = ticks };
            PlayerPrefs.SetString(fullKey, JsonConvert.SerializeObject(entry));
        }

        private static (bool Found, string Json) ReadValue(string fullKey)
        {
            var stored = PlayerPrefs.GetString(fullKey, "");
            if (string.IsNullOrWhiteSpace(stored))
                return (false, null);

            var entry = TryDeserialize(stored);
            return entry?.Value == null ? (true, stored) : (true, entry.Value);
        }

        private static long ReadTicks(string fullKey)
        {
            var entry = TryDeserialize(PlayerPrefs.GetString(fullKey, ""));
            return entry?.Value != null ? entry.Timestamp : 0;
        }

        private static TimestampedValue TryDeserialize(string stored)
        {
            if (string.IsNullOrWhiteSpace(stored))
                return null;

            try { return JsonConvert.DeserializeObject<TimestampedValue>(stored); }
            catch { return null; }
        }

        private static DateTime? TicksToTimestamp(long ticks) =>
            ticks > 0 ? new DateTime(ticks, DateTimeKind.Utc) : null;

        private static long TimestampToTicks(DateTime? timestamp) =>
            timestamp == null ? 0 : timestamp.Value.ToUniversalTime().Ticks;
    }
}
