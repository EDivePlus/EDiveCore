// Author: Michal Petr
// Created: 25.06.2026

using System;

namespace EDIVE.ServiceHub.SaveData
{
    public readonly struct StoreReadResult
    {
        public bool Found { get; }
        public string Json { get; }
        public DateTime? Timestamp { get; }

        private StoreReadResult(bool found, string json, DateTime? timestamp)
        {
            Found = found;
            Json = json;
            Timestamp = timestamp;
        }

        public static StoreReadResult NotFound => new(false, null, null);
        public static StoreReadResult Hit(string json, DateTime? timestamp) => new(true, json, timestamp);
    }

    public readonly struct TimestampPeek
    {
        public bool Found { get; }
        public DateTime? Timestamp { get; }

        private TimestampPeek(bool found, DateTime? timestamp)
        {
            Found = found;
            Timestamp = timestamp;
        }

        public static TimestampPeek Missing => new(false, null);
        public static TimestampPeek At(DateTime? timestamp) => new(true, timestamp);
    }
}
