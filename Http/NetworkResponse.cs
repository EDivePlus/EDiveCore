// Author: Michal Petr
// Created: 17.03.2026

namespace EDIVE.Http
{
    public readonly struct NetworkResponse<T>
    {
        public bool Success { get; }
        public long StatusCode { get; }
        public string Error { get; }
        public string Raw { get; }
        public T Result { get; }

        public bool IsNotFound => StatusCode == 404;

        private NetworkResponse(bool success, long statusCode, string error, string raw, T result)
        {
            Success = success;
            StatusCode = statusCode;
            Error = error;
            Raw = raw;
            Result = result;
        }

        public static NetworkResponse<T> Ok(long status, T result, string raw) => new(true, status, null, raw, result);
        public static NetworkResponse<T> Fail(long status, string error, string raw) => new(false, status, error, raw, default);
    }
}
