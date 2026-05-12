using System;
using PurrNet.Packing;

namespace Adrenak.UniVoice.Networks
{
    /// <summary>
    /// Broadcast payload exchanged between the PurrNet UniVoice client and server.
    /// The payload is a BRW-encoded byte stream that always begins with a tag
    /// (see <see cref="PurrNetBroadcastTags"/>) followed by tag-specific fields.
    /// </summary>
    [Serializable]
    public struct PurrNetBroadcast : IPackedAuto
    {
        public byte[] data;
    }
}
