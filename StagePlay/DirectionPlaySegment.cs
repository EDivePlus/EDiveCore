using System;
using UnityEngine;

namespace EDIVE.StagePlay
{
    [Serializable]
    public class DirectionPlaySegment : APlaySegment
    {
        [SerializeField]
        private string _Description;
        
        public string Description => _Description;
        
        public override bool IsOwnedByCharacter(string character) => true;

        public DirectionPlaySegment(string description)
        {
            _Description = description;
        }
    }
}
