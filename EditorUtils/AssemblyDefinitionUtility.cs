#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace EDIVE.EditorUtils
{
    public static class AssemblyDefinitionUtility
    {
        public static AssemblyDefinitionData GetData(this AssemblyDefinitionAsset asset)
        {
            return JsonConvert.DeserializeObject<AssemblyDefinitionData>(asset.text);
        }

        public static void SetData(this AssemblyDefinitionAsset asset, AssemblyDefinitionData data)
        {
            var assetPath = AssetDatabase.GetAssetPath(asset);
            File.WriteAllText(assetPath, SerializeData(data));
            EditorUtility.SetDirty(asset);
        }

        private static string SerializeData(AssemblyDefinitionData data)
        {
            var jsonSerializer = JsonSerializer.CreateDefault();
            jsonSerializer.Formatting = Formatting.Indented;

            var sb = new StringBuilder();
            var sw = new StringWriter(sb, CultureInfo.InvariantCulture);
            using (var jsonWriter = new JsonTextWriter(sw))
            {
                jsonWriter.Formatting = jsonSerializer.Formatting;
                jsonWriter.IndentChar = ' ';
                jsonWriter.Indentation = 4;
                jsonSerializer.Serialize(jsonWriter, data);
            }
            return sw.ToString();
        }
    }
    
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class AssemblyDefinitionData
    {
        [JsonProperty("name")] [SerializeField]
        private string _Name;

        [JsonProperty("rootNamespace")] [SerializeField]
        private string _RootNamespace;

        [JsonProperty("references")] [SerializeField]
        private string[] _References;

        [JsonProperty("includePlatforms")] [SerializeField]
        private string[] _IncludePlatforms;

        [JsonProperty("excludePlatforms")] [SerializeField]
        private string[] _ExcludePlatforms;

        [JsonProperty("allowUnsafeCode")] [SerializeField]
        private bool _AllowUnsafeCode;

        [JsonProperty("overrideReferences")] [SerializeField]
        private bool _OverrideReferences;

        [JsonProperty("precompiledReferences")] [SerializeField]
        private string[] _PrecompiledReferences;

        [JsonProperty("autoReferenced")] [SerializeField]
        private bool _AutoReferenced;

        [JsonProperty("defineConstraints")] [SerializeField]
        private string[] _DefineConstraints;

        [JsonProperty("versionDefines")] [SerializeField]
        private VersionDefineData[] _VersionDefines;

        [JsonProperty("noEngineReferences")] [SerializeField]
        private bool _NoEngineReferences;

        public string Name { get => _Name; set => _Name = value; }
        public string RootNamespace { get => _RootNamespace; set => _RootNamespace = value; }
        public string[] References { get => _References; set => _References = value; }
        public string[] IncludePlatforms { get => _IncludePlatforms; set => _IncludePlatforms = value; }
        public string[] ExcludePlatforms { get => _ExcludePlatforms; set => _ExcludePlatforms = value; }
        public bool AllowUnsafeCode { get => _AllowUnsafeCode; set => _AllowUnsafeCode = value; }
        public bool OverrideReferences { get => _OverrideReferences; set => _OverrideReferences = value; }
        public string[] PrecompiledReferences { get => _PrecompiledReferences; set => _PrecompiledReferences = value; }
        public bool AutoReferenced { get => _AutoReferenced; set => _AutoReferenced = value; }
        public string[] DefineConstraints { get => _DefineConstraints; set => _DefineConstraints = value; }
        public VersionDefineData[] VersionDefines { get => _VersionDefines; set => _VersionDefines = value; }
        public bool NoEngineReferences { get => _NoEngineReferences; set => _NoEngineReferences = value; }
    }

    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class VersionDefineData
    {
        [JsonProperty("name")] [SerializeField]
        private string _Name;

        [JsonProperty("expression")] [SerializeField]
        private string _Expression;

        [JsonProperty("define")] [SerializeField]
        private string _Define;

        public string Name { get => _Name; set => _Name = value; }
        public string Expression { get => _Expression; set => _Expression = value; }
        public string Define { get => _Define; set => _Define = value; }
    }
}
#endif
