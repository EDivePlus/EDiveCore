using System;
using System.Collections.Generic;
using UnityEngine;

namespace EDIVE.StagePlay
{
    [Serializable]
    public class SpeechPlaySegment : APlaySegment
    {
        [SerializeField]
        private List<string> _Characters;
        
        [SerializeField]
        private string _Line;
        
        public List<string> Characters => _Characters;
        public string Line => _Line;

        public SpeechPlaySegment(List<string> characters, string line)
        {
            _Characters = characters;
            _Line = line;
        }

        public override bool IsOwnedByCharacter(string character)
        {
            return _Characters.Contains(character);
        }
    }
}
