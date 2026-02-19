// Author: Radim Holub
// Created: 19.02.2026

using System;

namespace EDIVE.UserCenter
{
    public enum DataStatus
    {
        Ok,
        NotFound,
        Error
    }

    public readonly struct DataResult<T>
    {
        public DataStatus Status { get; }
        public T Value { get; }
        public string ErrorMessage { get; }
        public bool FromServer { get; }
        public bool FromLocal { get; }

        public bool IsOk => Status == DataStatus.Ok;
        public bool IsNotFound => Status == DataStatus.NotFound;

        private DataResult(DataStatus status, T value, string errorMessage, bool fromServer, bool fromLocal, bool fromMemory)
        {
            Status = status;
            Value = value;
            ErrorMessage = errorMessage;
            FromServer = fromServer;
            FromLocal = fromLocal;
        }

        public static DataResult<T> Ok(T value, bool fromServer, bool fromLocal, bool fromMemory = false)
            => new(DataStatus.Ok, value, null, fromServer, fromLocal, fromMemory);

        public static DataResult<T> NotFound()
            => new(DataStatus.NotFound, default, null, false, false, false);

        public static DataResult<T> Error(string error)
            => new(DataStatus.Error, default, error, false, false, false);
    }

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

        public static NetworkResponse<T> Ok(long status, T result, string raw)
            => new NetworkResponse<T>(true, status, null, raw, result);

        public static NetworkResponse<T> Fail(long status, string error, string raw)
            => new NetworkResponse<T>(false, status, error, raw, default);
    }
}


