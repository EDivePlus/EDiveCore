// Author: Michal Petr
// Created: 20.04.2026

using System.Collections.Generic;

namespace EDIVE.Http
{
    public readonly struct RequestStartedEvent
    {
        public long RequestId { get; }
        public string Method { get; }
        public string Url { get; }
        public string RequestPayload { get; }
        public Dictionary<string, string> RequestHeaders { get; }

        public RequestStartedEvent(long requestId, string method, string url, string requestPayload, Dictionary<string, string> requestHeaders)
        {
            RequestId = requestId;
            Method = method;
            Url = url;
            RequestPayload = requestPayload;
            RequestHeaders = requestHeaders;
        }
    }

    public readonly struct RequestCompletedEvent
    {
        public long RequestId { get; }
        public long StatusCode { get; }
        public bool IsSuccess { get; }
        public string ResponsePayload { get; }
        public string ErrorMessage { get; }
        public Dictionary<string, string> ResponseHeaders { get; }

        public RequestCompletedEvent(long requestId, long statusCode, bool isSuccess, string responsePayload, string errorMessage, Dictionary<string, string> responseHeaders)
        {
            RequestId = requestId;
            StatusCode = statusCode;
            IsSuccess = isSuccess;
            ResponsePayload = responsePayload;
            ErrorMessage = errorMessage;
            ResponseHeaders = responseHeaders;
        }
    }

    public readonly struct RequestCancelledEvent
    {
        public long RequestId { get; }

        public RequestCancelledEvent(long requestId)
        {
            RequestId = requestId;
        }
    }
}
