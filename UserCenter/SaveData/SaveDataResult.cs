// Author: Michal Petr
// Created: 17.03.2026

using System;

namespace EDIVE.UserCenter.SaveData
{
    [Flags]
    public enum SaveDataStatus
    {
        Error = 0,
        SavedLocal,
        SavedRemote,
        Saved = SavedLocal | SavedRemote,
    }
    
    public readonly struct SaveDataResult<T>
    {
        public bool IsSuccess { get; }
        public T Value { get; }
        public string ErrorMessage { get; }
        public bool FromRemote { get; }
        
        public bool IsError => !IsSuccess;
        public bool FromLocal => !FromRemote;

        private SaveDataResult(bool isSuccess, T value, string errorMessage, bool fromRemote)
        {
            IsSuccess = isSuccess;
            Value = value;
            ErrorMessage = errorMessage;
            FromRemote = fromRemote;
        }

        public static SaveDataResult<T> Success(T value, bool fromServer)
            => new(true, value, null, fromServer);

        public static SaveDataResult<T> Error(string error, T value = default)
            => new(false, value, error, false);
    }
}
