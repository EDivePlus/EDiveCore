// Author: František Holubec
// Created: 14.04.2025

using System;
using UnityEngine;

namespace EDIVE.AssetTranslation
{
    [Serializable]
    public class DefinitionMigrationPreset<TDefinition> where TDefinition : ScriptableObject, IUniqueDefinition
    {
        [SerializeField]
        private string _OriginalID;

        [SerializeField]
        private TDefinition _Definition;

        public string OriginalID => _OriginalID;
        public TDefinition Definition => _Definition;

        public DefinitionMigrationPreset(string originalID, TDefinition definition)
        {
            _OriginalID = originalID;
            _Definition = definition;
        }

        protected bool Equals(DefinitionMigrationPreset<TDefinition> other)
        {
            return _OriginalID == other._OriginalID;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DefinitionMigrationPreset<TDefinition>) obj);
        }

        public override int GetHashCode()
        {
            return (_OriginalID != null ? _OriginalID.GetHashCode() : 0);
        }
    }
}
