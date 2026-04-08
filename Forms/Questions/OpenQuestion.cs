// Author: Michal Petr
// Created: 05.11.2025

using System;
using UnityEngine;

namespace EDIVE.Forms.Questions
{
    [Serializable]
    public class OpenQuestion : AFormQuestion
    {
        public override string EditorLabel => "Open";
        
        [SerializeField]
        private ChangeTriggerType _ChangeTrigger;
        public ChangeTriggerType ChangeTrigger => _ChangeTrigger;

        public enum ChangeTriggerType
        {
            ValueChanged,
            EndEdit
        }
    }
}
