// Author: Michal Petr
// Created: 17.03.2026

namespace EDIVE.UserCenter.SaveData
{
    public enum SaveDataStatus
    {
        Ok,
        NotFound,
        Error
    }
    
    public readonly struct SaveDataResult<T>
    {
        public SaveDataStatus Status { get; }
        public T Value { get; }
        public string ErrorMessage { get; }
        public bool FromRemote { get; }

        public bool IsOk => Status == SaveDataStatus.Ok;
        public bool IsNotFound => Status == SaveDataStatus.NotFound;
        public bool FromLocal => !FromRemote;

        private SaveDataResult(SaveDataStatus status, T value, string errorMessage, bool fromRemote)
        {
            Status = status;
            Value = value;
            ErrorMessage = errorMessage;
            FromRemote = fromRemote;
        }

        public static SaveDataResult<T> Ok(T value, bool fromServer)
            => new(SaveDataStatus.Ok, value, null, fromServer);

        public static SaveDataResult<T> NotFound()
            => new(SaveDataStatus.NotFound, default, null, false);

        public static SaveDataResult<T> Error(string error, T value = default)
            => new(SaveDataStatus.Error, value, error, false);
    }
}
