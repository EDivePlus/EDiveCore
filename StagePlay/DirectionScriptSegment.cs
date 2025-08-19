using System;
using EDIVE.Utils.Json.TypeNames;
using Newtonsoft.Json;
using UnityEngine;

namespace EDIVE.StagePlay
{
    [Serializable]
    [JsonTypeName("direction")]
    public class DirectionScriptSegment : AScriptSegment
    {
        [SerializeField]
        [JsonProperty("description")]
        private string _Description;
        
        public string Description => _Description;
    }
}
