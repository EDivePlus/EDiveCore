using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EDIVE.EditorUtils;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.Utils.Json;
using JetBrains.Annotations;
using Mirror;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.Validation;
using UnityEditor;
using UnityEngine;

namespace EDIVE.AssetTranslation
{
    public abstract class ADefinitionTranslator : ScriptableObject
    {
        public abstract bool RequireAllAssets { get; }

        public abstract IReadOnlyList<ScriptableObject> BaseDefinitions { get; }
        public abstract Type DefinitionType { get; }

        public abstract bool Contains(ScriptableObject definition);
        public abstract bool Contains(string uniqueID);

        public abstract bool TryGet(string uniqueID, out ScriptableObject resultDefinition);
        public abstract bool TryGet(Predicate<ScriptableObject> predicate, out ScriptableObject resultDefinition);

        public abstract bool Add(ScriptableObject definition);
        public abstract bool Remove(ScriptableObject definition);

        public abstract bool TryAddMigrationPreset(string id, ScriptableObject definition);

        public abstract JObject Export(JsonSerializer serializer);
        public abstract void CreateMissing(JsonSerializer serializer, JObject itemCollectionJObject);
        public abstract void Populate(JsonSerializer serializer, JObject itemCollectionJObject);
    }

    public abstract class ADefinitionTranslator<TDefinition> : ADefinitionTranslator where TDefinition : ScriptableObject, IUniqueDefinition
    {
        [EnhancedValidate("ValidateDefinitions", IncludeChildren = true)]
        [EnhancedAssetList(Path = "$FolderPathFilter")]
        [PropertyOrder(-10)]
        [SerializeField]
        protected List<TDefinition> _Definitions;

        [EnhancedFoldoutGroup("Migration", "@OdinColors.Orange", SpaceBefore = 6)]
        [EnhancedTableList(ShowFoldout = false)]
        [SerializeField]
        private List<DefinitionMigrationPreset<TDefinition>> _MigrationPresets = new();

        [SerializeField]
        protected bool _RequireAllAssets = true;

        [FolderPath]
        [PropertyOrder(-10)]
        [EnhancedBoxGroup("Filter", "@ColorTools.Cyan", SpaceAfter = 4)]
        [ListDrawerSettings(ShowFoldout = false)]
        [LabelText("Folders")]
        [SerializeField]
        protected string[] _FilterFolders;

        public override bool RequireAllAssets => _RequireAllAssets;
        public override Type DefinitionType => typeof(TDefinition);
        public override IReadOnlyList<ScriptableObject> BaseDefinitions => _Definitions;
        public IReadOnlyList<TDefinition> Definitions => _Definitions ??= new List<TDefinition>();

        public bool Contains(TDefinition definition)
        {
            return Definitions.Contains(definition);
        }

        public override bool Contains(ScriptableObject definition)
        {
            return definition is TDefinition tDefinition && Contains(tDefinition);
        }

        public override bool Contains(string uniqueID)
        {
            return Definitions.Any(d => d.UniqueID == uniqueID);
        }

        public List<TSpecificDefinition> GetAll<TSpecificDefinition>(Predicate<TSpecificDefinition> filter = null) where TSpecificDefinition : TDefinition
        {
            var items = new List<TSpecificDefinition>();
            foreach (var definition in Definitions)
            {
                if (definition is TSpecificDefinition tDef && (filter == null || filter(tDef)))
                    items.Add(tDef);
            }
            return items;
        }

        public List<TDefinition> GetAll(Predicate<TDefinition> filter = null)
        {
            var items = new List<TDefinition>();
            foreach (var definition in Definitions)
            {
                if (filter != null && !filter(definition)) continue;
                items.Add(definition);
            }
            return items;
        }

        public override bool TryGet(string uniqueID, out ScriptableObject resultDefinition)
        {
            if (TryGet(uniqueID, out var resultValue))
            {
                resultDefinition = resultValue;
                return true;
            }

            resultDefinition = null;
            return false;
        }

        public override bool TryGet(Predicate<ScriptableObject> predicate, out ScriptableObject resultDefinition)
        {
            return Definitions.TryGetFirst(predicate, out resultDefinition);
        }

        public bool TryGet(string uniqueID, out TDefinition resultDefinition)
        {
            resultDefinition = null;
            if (string.IsNullOrWhiteSpace(uniqueID))
                return false;

            if (TryGet(d => d.UniqueID == uniqueID, out resultDefinition))
                return true;

            if (_MigrationPresets.TryGetFirst(p => p != null && p.OriginalID == uniqueID && p.Definition != null, out var preset))
            {
                resultDefinition = preset.Definition;
                return true;
            }

            return false;
        }

        public bool TryGet(Predicate<TDefinition> predicate, out TDefinition resultDefinition)
        {
            return Definitions.TryGetFirst(predicate, out resultDefinition);
        }

        public virtual bool Add(TDefinition definition)
        {
            if (Definitions.Contains(definition))
                return false;
            _Definitions.Add(definition);
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
            return true;
        }

        public bool Remove(TDefinition definition)
        {
            if (!_Definitions.Remove(definition))
                return false;
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
            return true;
        }

        public override bool Add(ScriptableObject definition)
        {
            return definition is TDefinition tDefinition && Add(tDefinition);
        }

        public override bool Remove(ScriptableObject definition)
        {
            return definition is TDefinition tDefinition && Remove(tDefinition);
        }

        public override bool TryAddMigrationPreset(string id, ScriptableObject definition)
        {
            return definition is TDefinition tDefinition && TryAddMigrationPreset(id, tDefinition);
        }

        public bool TryAddMigrationPreset(string id, TDefinition definition)
        {
            if(_MigrationPresets.TryGetFirst(p => p.OriginalID == id, out var def))
            {
                return false;
            }

            _MigrationPresets.Add(new DefinitionMigrationPreset<TDefinition>(id, definition));
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
            return true;
        }

        public override JObject Export(JsonSerializer serializer)
        {
            return JsonAssetUtils.SerializeAssets(serializer, Definitions, TryGetDefinitionID, typeof(TDefinition));

            bool TryGetDefinitionID(TDefinition definition, out string resultId)
            {
                resultId = definition.UniqueID;
                return true;
            }
        }

        public override void CreateMissing(JsonSerializer serializer, JObject itemCollectionJObject)
        {
            JsonAssetUtils.CreateMissingAssets<TDefinition>(serializer, itemCollectionJObject, Contains, OnMissingDefinitionCreated);
            return;

            void OnMissingDefinitionCreated(string itemID, JObject jObject, TDefinition definition)
            {
                if (definition == null)
                    return;

                definition.UniqueID = itemID;
                Add(definition);
            }
        }

        public override void Populate(JsonSerializer serializer, JObject itemCollectionJObject)
        {
            JsonAssetUtils.PopulateAssets<TDefinition>(serializer, itemCollectionJObject, TryGet);
        }

#if UNITY_EDITOR
        [UsedImplicitly]
        public string FolderPathFilter => _FilterFolders != null ? string.Join(";", _FilterFolders) : null;

        [UsedImplicitly]
        protected virtual void ValidateDefinitions(List<TDefinition> definitions, ValidationResult result, InspectorProperty property)
        {
            if (definitions == null)
                return;

            if (_RequireAllAssets)
            {
                var allAssets = EditorAssetUtils.FindAllAssetsOfType<TDefinition>(_FilterFolders);
                var missingAssets = allAssets.Except(definitions).ToList();
                if (missingAssets.Count > 0)
                {
                    result.AddError("There are missing definitions!").WithFix(() =>
                    {
                        _Definitions.AddRange(missingAssets);
                        property.ForceMarkDirty();
                    });
                }
            }

            var emptyIdDefs = definitions.FindAll(d => d != null && string.IsNullOrEmpty(d.UniqueID)).ToList();
            if (emptyIdDefs.Count > 0)
            {
                result.AddError($"There are definitions with empty ID: {string.Join(", ", emptyIdDefs)}").WithFix(() =>
                {
                    foreach (var definition in emptyIdDefs)
                    {
                        definition.SetFileNameAsID();
                    }
                    property.ForceMarkDirty();
                });
            }

            var duplicateDefinitions = definitions
                .Where(d => d != null)
                .GroupBy(d => d)
                .Where(g => g.Count() > 1)
                .ToList();

            if (duplicateDefinitions.Count > 0)
            {
                var builder = new StringBuilder();
                builder.Append($"There are multiple references to the same definitions: [{string.Join(", ", duplicateDefinitions.Select(s => s.Key.name))}]");
                result.AddError(builder.ToString());
            }

            var duplicateSaveIDs = definitions
                .Where(d => d != null)
                .GroupBy(d => d.UniqueID)
                .Where(g => g.Count() > 1)
                .ToList();

            if (duplicateSaveIDs.Count > 0)
            {
                var builder = new StringBuilder();
                builder.Append("There are multiple definitions with the same ID:");
                foreach (var duplicateSaveID in duplicateSaveIDs)
                {
                    builder.Append($"\n- ID:'{duplicateSaveID.Key}' - [{string.Join(", ", duplicateSaveID.ToList().Select(s => s.name))}]");
                }
                result.AddError(builder.ToString());
            }
        }

        [Button]
        private void SetFileNameAsIDForAll()
        {
            foreach (var definition in Definitions)
            {
                definition.SetFileNameAsID();
            }
        }
#endif
    }
}



