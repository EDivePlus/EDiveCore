// Author: František Holubec
// Created: 05.05.2026

using System;
using EDIVE.AppLoading.LoadItems;
using EDIVE.Conditions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.AppLoading.Utils
{
    [Serializable]
    public class LoadItemAvailableCondition : ABoolCondition
    {
        [Required]
        [SerializeField]
        private LoadItemDefinition _LoadItem;

        protected override bool GetValue()
        {
            return _LoadItem == null || _LoadItem.CheckAvailability();
        }
    }
}
