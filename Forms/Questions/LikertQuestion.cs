// Author: Michal Petr
// Created: 05.11.2025

using System;
using System.Collections.Generic;
using EDIVE.OdinExtensions.Attributes;
using UnityEngine;

namespace EDIVE.Forms.Questions
{
    [Serializable]
    public class LikertQuestion : AFormQuestion
    {
        public override string EditorLabel => "Likert";
        
        [SerializeField]
        private int _Scale = 5;
        
        [EnhancedTableList]
        [SerializeField]
        private List<LikertAnchor> _Anchors = new();

        public int Scale => _Scale;
        public IReadOnlyList<LikertAnchor> Anchors => _Anchors;
       
    }

    [Serializable]
    public class LikertAnchor
    {
        [EnhancedTableColumn(40)]
        [SerializeField]
        private int _Value;
        
        [SerializeField]
        private string _Label;

        public int Value => _Value;
        public string Label => _Label;
    }
}
