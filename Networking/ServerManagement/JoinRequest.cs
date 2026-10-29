// Author: Michal Petr
// Created: 20.05.2026

namespace EDIVE.Networking.ServerManagement
{
    public interface IJoinRequest
    {
        
    }

    public struct DirectJoinRequest : IJoinRequest
    {
        public string Address;
        public ushort Port;

        public DirectJoinRequest(string address, ushort port)
        {
            Address = address;
            Port = port;
        }
    }

    public struct CodeJoinRequest : IJoinRequest
    {
        public string Code;

        public CodeJoinRequest(string code)
        {
            Code = code;
        }
    }

    public enum JoinError
    {
        None,
        InvalidInput,
        NotFound,
        TooManyAttempts,
        ServiceOff,
        Banned,
        Unreachable,
        ConnectFailed,
        Unknown
    }

    // Adapter answer to a join request: not handled, a server to connect to, or why it failed
    public readonly struct JoinResult
    {
        public readonly ServerRecord Record;
        public readonly JoinError Error;

        public bool Handled => Record != null || Error != JoinError.None;

        private JoinResult(ServerRecord record, JoinError error)
        {
            Record = record;
            Error = error;
        }

        public static JoinResult NotHandled => default;
        public static JoinResult Found(ServerRecord record) => new(record, JoinError.None);
        public static JoinResult Failed(JoinError error) => new(null, error);
    }
}
