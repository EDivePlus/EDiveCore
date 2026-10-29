// Author: Michal Petr
// Created: 18.05.2026

namespace EDIVE.ServiceHub
{
    // Mirror of the backend ApiStatus enum (ServiceHub/src/Models/ApiStatus.cs).
    // Values come back in the ApiResponse "status" field and are surfaced via NetworkResponse.ApiStatus.
    // Keep in sync with the backend if it changes.
    public enum ServiceHubStatus
    {
        Success = 0,
        MissingArgument = 9,
        CodeTaken = 10,
        AppNotFound = 11,
        InvalidSecret = 12,
        ServerNotFound = 13,
        InvalidCredentials = 14,
        InvalidQrCode = 15,
        KeyTooLong = 16,
        ValueTooLarge = 17,
        SaveDataNotFound = 18,
        Unauthorized = 19,
        ContentItemNotFound = 20,
        InvalidDomain = 21,
        InvalidRefreshToken = 22,
        AccountAlreadyLinked = 23,
        RegistrationFailed = 24,
        SaveDataQuotaExceeded = 25,
        BatchTooLarge = 26,
        BannedFromApp = 27,
        StreamingUnavailable = 28,
        PresenceNotFound = 29,
        NotSessionPlayer = 30,
        CommandNotFound = 31,
        ServiceDisabled = 32,
        DeviceCodePending = 33,
        DeviceCodeDenied = 34,
        DeviceCodeExpired = 35,
        EmailNotConfirmed = 36
    }

    // Failure kinds the UI has a message for
    public enum ServiceHubError
    {
        Failed,
        Unreachable,
        TooManyRequests,
        SignedOut,
        ServiceOff,
        NotFound
    }

    // Status groups shared by the UI messages
    public static class ServiceHubErrors
    {
        public static ServiceHubError Classify(long statusCode, int apiStatus) => apiStatus switch
        {
            (int) ServiceHubStatus.ServiceDisabled => ServiceHubError.ServiceOff,
            (int) ServiceHubStatus.ContentItemNotFound => ServiceHubError.NotFound,
            _ when IsSignedOut(statusCode, apiStatus) => ServiceHubError.SignedOut,
            _ when IsRateLimited(statusCode) => ServiceHubError.TooManyRequests,
            _ when IsUnreachable(statusCode) => ServiceHubError.Unreachable,
            _ => ServiceHubError.Failed
        };

        public static bool IsUnreachable(long statusCode) => statusCode is 0 or >= 500;
        public static bool IsRateLimited(long statusCode) => statusCode == 429;

        public static bool IsSignedOut(long statusCode, int apiStatus) =>
            statusCode == 401 || (ServiceHubStatus) apiStatus is ServiceHubStatus.Unauthorized or ServiceHubStatus.InvalidRefreshToken;
    }
}
