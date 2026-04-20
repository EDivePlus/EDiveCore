using System;
using System.Collections.Generic;
using UnityEditor;

namespace EDIVE.Http.Editor
{
    public enum RequestStatus
    {
        Pending,
        Success,
        Failed,
        Cancelled
    }

    public class NetworkRequestLog
    {
        public long Id;
        public string Method;
        public string Url;
        public string RequestPayload;
        public DateTime StartTime;
        public DateTime? EndTime;
        public RequestStatus Status;
        public long StatusCode;
        public string ResponsePayload;
        public string ErrorMessage;
        public Dictionary<string, string> RequestHeaders = new();
        public Dictionary<string, string> ResponseHeaders = new();

        public double DurationMs => EndTime.HasValue
            ? (EndTime.Value - StartTime).TotalMilliseconds
            : (DateTime.Now - StartTime).TotalMilliseconds;
    }

    public static class NetworkRequestLogger
    {
        // ReSharper disable once InconsistentNaming
        private static readonly List<NetworkRequestLog> _logs = new();
        private static readonly Dictionary<long, NetworkRequestLog> _logsByRequestId = new();

        public static IReadOnlyList<NetworkRequestLog> Logs => _logs;

        public static event Action<NetworkRequestLog> OnLogAdded;
        public static event Action<NetworkRequestLog> OnLogUpdated;
        public static event Action OnLogsCleared;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            RestUtils.OnRequestStarted += HandleRequestStarted;
            RestUtils.OnRequestCompleted += HandleRequestCompleted;
            RestUtils.OnRequestCancelled += HandleRequestCancelled;
        }

        private static void HandleRequestStarted(RequestStartedEvent e)
        {
            var log = new NetworkRequestLog
            {
                Id = e.RequestId,
                Method = e.Method,
                Url = e.Url,
                RequestPayload = e.RequestPayload,
                RequestHeaders = e.RequestHeaders ?? new Dictionary<string, string>(),
                StartTime = DateTime.Now,
                Status = RequestStatus.Pending
            };
            _logs.Add(log);
            _logsByRequestId[e.RequestId] = log;
            OnLogAdded?.Invoke(log);
        }

        private static void HandleRequestCompleted(RequestCompletedEvent e)
        {
            if (!_logsByRequestId.TryGetValue(e.RequestId, out var log)) return;
            log.EndTime = DateTime.Now;
            log.StatusCode = e.StatusCode;
            log.Status = e.IsSuccess ? RequestStatus.Success : RequestStatus.Failed;
            log.ResponsePayload = e.ResponsePayload;
            log.ErrorMessage = e.ErrorMessage;
            if (e.ResponseHeaders != null)
                log.ResponseHeaders = e.ResponseHeaders;
            OnLogUpdated?.Invoke(log);
        }

        private static void HandleRequestCancelled(RequestCancelledEvent e)
        {
            if (!_logsByRequestId.TryGetValue(e.RequestId, out var log)) return;
            log.EndTime = DateTime.Now;
            log.Status = RequestStatus.Cancelled;
            log.ErrorMessage = "Cancelled";
            OnLogUpdated?.Invoke(log);
        }

        public static void Clear()
        {
            _logs.Clear();
            _logsByRequestId.Clear();
            OnLogsCleared?.Invoke();
        }
    }
}

