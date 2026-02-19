using System;
using EDIVE.Utils.Json.TypeNames;
using Newtonsoft.Json;
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
    }
}
