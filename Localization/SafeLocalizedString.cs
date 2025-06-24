using System;
using Newtonsoft.Json;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace EDIVE.Localization
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    public class SafeLocalizedString : LocalizedString
    {
        public const string UNDEFINED_TERM = "Undefined Term";
        public const string NOT_LOADED_TERM = "Term not loaded";

        public static readonly SafeLocalizedString UNDEFINED = new("Invalid", UNDEFINED_TERM);
        public static readonly SafeLocalizedString EMPTY = new((string) null, null)
        {
            _fallbackValue = string.Empty
        };

        [JsonProperty("Fallback", DefaultValueHandling = DefaultValueHandling.Ignore)]
        private string _fallbackValue;

        [JsonProperty("Term")]
        public string Term
        {
            get => this.GetTerm();
            set => this.SetTerm(value);
        }

        public SafeLocalizedString() { }
        
        public SafeLocalizedString(string term)
        {
            this.SetTerm(term);
        }
        
        public SafeLocalizedString(TableReference table, TableEntryReference entry)
        {
            TableReference = table;
            TableEntryReference = entry;
        }
        
        public SafeLocalizedString(string table, string entry)
        {
            TableReference = table;
            TableEntryReference = entry;
        }

        public override string ToString() => this.GetLocalizedStringSafe(_fallbackValue);
        public static implicit operator string(SafeLocalizedString s) => s.ToString();
    }
}