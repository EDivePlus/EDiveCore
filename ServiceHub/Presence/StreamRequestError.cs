// Author: František Holubec
// Created: 28.10.2026

namespace EDIVE.ServiceHub.Presence
{
    public enum StreamRequestError
    {
        None,
        NotSessionPlayer,
        Unavailable,
        PlayerOffline,
        ServiceOff,
        SignedOut,
        TooManyRequests,
        Unreachable,
        Declined,
        NoAnswer,
        Failed
    }

    // Outcome of a stream request, Error says why it failed
    public readonly struct StreamActionResult
    {
        public readonly string Result;
        public readonly string Message;
        public readonly StreamRequestError Error;

        public StreamActionResult(string result, string message, StreamRequestError error)
        {
            Result = result;
            Message = message;
            Error = error;
        }

        public static StreamActionResult From(StreamActionResponse response) => new(response.Result, response.Message, StreamRequestError.None);
        public static StreamActionResult Failed(StreamRequestError error, string message = null) => new(PresenceResult.FAILED, message, error);
    }

    public static class StreamRequestErrors
    {
        public static StreamRequestError FromStatus(long statusCode, int apiStatus) => (ServiceHubStatus) apiStatus switch
        {
            ServiceHubStatus.NotSessionPlayer => StreamRequestError.NotSessionPlayer,
            ServiceHubStatus.StreamingUnavailable => StreamRequestError.Unavailable,
            ServiceHubStatus.PresenceNotFound => StreamRequestError.PlayerOffline,
            ServiceHubStatus.ServiceDisabled => StreamRequestError.ServiceOff,
            _ when ServiceHubErrors.IsSignedOut(statusCode, apiStatus) => StreamRequestError.SignedOut,
            _ when ServiceHubErrors.IsRateLimited(statusCode) => StreamRequestError.TooManyRequests,
            _ when ServiceHubErrors.IsUnreachable(statusCode) => StreamRequestError.Unreachable,
            _ => StreamRequestError.Failed
        };
    }
}
