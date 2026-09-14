// Author: František Holubec
// Created: 14.09.2026

using System;
using System.Collections.Generic;

namespace EDIVE.CredentialStore
{
    internal sealed class UnsupportedCredentialStore : ICredentialStore
    {
        private readonly string _platform;

        public UnsupportedCredentialStore(string platform)
        {
            _platform = platform;
        }

        public string Name => $"Unsupported ({_platform})";

        public CredentialResult CheckAvailability() => Unavailable();

        public CredentialResult Get(string service, string account, out string secret)
        {
            secret = null;
            return Unavailable();
        }

        public CredentialResult Contains(string service, string account) => Unavailable();

        public CredentialResult Set(string service, string account, string secret) => Unavailable();

        public CredentialResult Delete(string service, string account) => Unavailable();

        public CredentialResult List(out IReadOnlyList<CredentialEntry> entries)
        {
            entries = Array.Empty<CredentialEntry>();
            return Unavailable();
        }

        private CredentialResult Unavailable() => CredentialResult.Unavailable($"No store for {_platform}.");
    }
}
