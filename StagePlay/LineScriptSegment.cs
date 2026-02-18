using System;
using System.Collections.Generic;
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
        private List<string> _Characters;
        
        [SerializeField]
        [JsonProperty("line")]
        private string _Line;
        
        public List<string> Characters => _Characters;
        public string Line => _Line;
        
        public override bool IsOwnedByCharacter(string character)
        {
            return _Characters.Contains(character);
        }
    }
}
