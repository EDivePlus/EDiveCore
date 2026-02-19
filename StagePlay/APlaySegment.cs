using System;
using Newtonsoft.Json;

namespace EDIVE.StagePlay
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class APlaySegment
    {
        public abstract bool IsOwnedByCharacter(string character);
    }
}