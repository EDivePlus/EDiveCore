using System;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.StagePlay
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class StagePlaySegment
    {
        [SerializeField]
        private StagePlaySegmentType _Type;
        
        [ShowIf(nameof(_Type), StagePlaySegmentType.Speach)]
        [SerializeField]
        private string _Characters;
        
        [SerializeField]
        private string _Line;
        
        public StagePlaySegmentType Type => _Type;
        public string Characters => _Characters;
        public string Line => _Line;

        public StagePlaySegment(StagePlaySegmentType type,string line, string characters)
        {
            _Type = type;
            _Line = line;
            _Characters = characters;
        }
    }
}