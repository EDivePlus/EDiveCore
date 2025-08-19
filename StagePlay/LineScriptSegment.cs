using System;
using EDIVE.Utils.Json.TypeNames;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.StagePlay
{
    [Serializable]
    [JsonTypeName("line")]
    public class LineScriptSegment : AScriptSegment
    {
        [SerializeField]
        [JsonProperty("characters")]
        private string[] _Characters;
        
        [SerializeField]
        [JsonProperty("line")]
        private string _Line;
        
        public string[] Characters => _Characters;
        public string Line => _Line;
    }
}
