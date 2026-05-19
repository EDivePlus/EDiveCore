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
}
