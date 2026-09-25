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

        public CredentialResult CheckAvailability() => NotSupported();

        public CredentialResult Get(string service, string account, out string secret)
        {
            secret = null;
            return NotSupported();
        }

        public CredentialResult Contains(string service, string account) => NotSupported();

        public CredentialResult Set(string service, string account, string secret) => NotSupported();

        public CredentialResult Delete(string service, string account) => NotSupported();

        public CredentialResult List(out IReadOnlyList<CredentialEntry> entries)
        {
            entries = Array.Empty<CredentialEntry>();
            return NotSupported();
        }

        // NotSupported is not logged as error
        private CredentialResult NotSupported() => CredentialResult.NotSupported($"No store for {_platform}.");
    }
}
