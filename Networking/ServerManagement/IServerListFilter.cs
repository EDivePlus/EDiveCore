// Author: Michal Petr
// Created: 21.09.2026

namespace EDIVE.Networking.ServerManagement
{
    public interface IServerListFilter
    {
        bool Matches(ServerRecord record);
    }
}
