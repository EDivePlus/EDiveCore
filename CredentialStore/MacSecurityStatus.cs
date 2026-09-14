// Author: František Holubec
// Created: 14.09.2026

namespace EDIVE.CredentialStore
{
    internal static class MacSecurityStatus
    {
        public const int SUCCESS = 0;
        public const int USER_CANCELED = -128;
        public const int AUTH_FAILED = -25293;
        public const int NO_SUCH_KEYCHAIN = -25294;
        public const int INVALID_KEYCHAIN = -25295;
        public const int DUPLICATE_ITEM = -25299;
        public const int ITEM_NOT_FOUND = -25300;
        public const int INTERACTION_NOT_ALLOWED = -25308;
        public const int INTERACTION_REQUIRED = -25315;

        public static CredentialResult ToResult(int status) => status switch
        {
            SUCCESS => CredentialResult.Ok,
            ITEM_NOT_FOUND => CredentialResult.NotFound,
            USER_CANCELED or AUTH_FAILED => CredentialResult.Denied($"Keychain denied ({status})."),
            INTERACTION_NOT_ALLOWED or INTERACTION_REQUIRED => CredentialResult.Unavailable($"Keychain locked, no prompt ({status})."),
            NO_SUCH_KEYCHAIN or INVALID_KEYCHAIN => CredentialResult.Unavailable($"No keychain ({status})."),
            _ => CredentialResult.Failed($"Keychain error ({status}).")
        };
    }
}
