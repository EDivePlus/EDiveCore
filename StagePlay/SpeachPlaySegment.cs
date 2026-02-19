using System;
using System.Collections.Generic;
using UnityEngine;

namespace EDIVE.StagePlay
{
    [Serializable]
    public class SpeachPlaySegment : APlaySegment
    {
        [SerializeField]
        private List<string> _Characters;
        
        [SerializeField]
        private string _Line;
        
        public List<string> Characters => _Characters;
        public string Line => _Line;
        
        public override bool IsOwnedByCharacter(string character)
        {
            return _Characters.Contains(character);
        }
    }
}
