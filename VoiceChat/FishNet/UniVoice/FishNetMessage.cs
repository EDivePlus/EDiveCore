#if FISHNET
using System;
using FishNet.Broadcast;

namespace Adrenak.UniVoice.Networks
{
    [Serializable]
    public struct FishNetMessage : IBroadcast
    {
        public byte[] data;
    }
}
#endif
