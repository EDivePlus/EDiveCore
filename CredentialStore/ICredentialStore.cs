// Author: František Holubec
// Created: 14.09.2026

using System.Collections.Generic;

namespace EDIVE.CredentialStore
{
    public interface ICredentialStore
    {
        string Name { get; }

        // Whether the backing store can be reached at all, separate from whether it holds a given entry.
        CredentialResult CheckAvailability();

        CredentialResult Get(string service, string account, out string secret);
        CredentialResult Contains(string service, string account);
        CredentialResult Set(string service, string account, string secret);
        CredentialResult Delete(string service, string account);
        CredentialResult List(out IReadOnlyList<CredentialEntry> entries);
    }
}
