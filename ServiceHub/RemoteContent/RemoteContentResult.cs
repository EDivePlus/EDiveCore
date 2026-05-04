// Author: Michal Petr
// Created: 04.05.2026

namespace EDIVE.ServiceHub.RemoteContent
{
    public readonly struct RemoteContentResult
    {
        public byte[] Bytes { get; }

        public RemoteContentResult(byte[] bytes)
        {
            Bytes = bytes;
        }
    }
}
