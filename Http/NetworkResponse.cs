// Author: Michal Petr
// Created: 17.03.2026

namespace EDIVE.Http
{
    public readonly struct NetworkResponse<T>
    {
        public bool IsSuccess { get; }
        public long StatusCode { get; }
        public string ErrorMessage { get; }
        public T Result { get; }

        public bool IsNotFound => StatusCode == 404;

        private NetworkResponse(bool isSuccess, long statusCode, string errorMessage, T result)
        {
            IsSuccess = isSuccess;
            StatusCode = statusCode;
            ErrorMessage = errorMessage;
            Result = result;
        }

        public static NetworkResponse<T> Success(long status, T result) => new(true, status, null, result);
        public static NetworkResponse<T> Error(long status, string error) => new(false, status, error, default);
    }
    
    public readonly struct RawNetworkResponse
    {
        public bool IsSuccess { get; }
        public long StatusCode { get; }
        public string ErrorMessage { get; }
        public string Raw { get; }

        public bool IsNotFound => StatusCode == 404;

        private RawNetworkResponse(bool isSuccess, long statusCode, string errorMessage, string raw)
        {
            IsSuccess = isSuccess;
            StatusCode = statusCode;
            ErrorMessage = errorMessage;
            Raw = raw;
        }

        public static RawNetworkResponse Success(long status, string raw) => new(true, status, null, raw);
        public static RawNetworkResponse Error(long status, string error, string raw) => new(false, status, error, raw);
    }
}
