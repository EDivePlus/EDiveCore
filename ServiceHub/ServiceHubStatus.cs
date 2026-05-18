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
        ContentItemNotFound = 20
    }
}
