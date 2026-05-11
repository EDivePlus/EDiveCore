using System;
using EDIVE.AppLoading.LoadItems;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.AppLoading.Dependencies
{
    [Serializable]
    public class LoadItemDependency 
    {
        [Required]
        [SerializeField]
        private LoadItemDefinition _Definition;

        [EnhancedTableColumn(60)]
        [SerializeField]
        private bool _Optional;

        public LoadItemDefinition Definition => _Definition;
        public bool IsOptional => _Optional;

        public LoadItemDependency() { }
        public LoadItemDependency(LoadItemDefinition definition)
        {
            _Definition = definition;
        }
    }
}
