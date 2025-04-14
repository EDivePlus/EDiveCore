using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace EDIVE.AssetTranslation
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class AUniqueDefinition : ScriptableObject, IUniqueDefinition
    {
        [Required]
        [PropertyOrder(-100)]
        [UniqueDefinitionID("FormatFileNameForID")]
        [SerializeField]
        protected string _UniqueID;

        public string UniqueID
        {
            get => _UniqueID;
            set
            {
                _UniqueID = value;
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }
        }

        protected virtual void Awake()
        {
#if UNITY_EDITOR
            EnsureID();
#endif
        }

#if UNITY_EDITOR
        public virtual void SetFileNameAsID()
        {
            _UniqueID = FormatFileNameForID(name);
            EditorUtility.SetDirty(this);
        }

        protected virtual string FormatFileNameForID(string filename) => filename;

        [OnInspectorInit]
        private void EnsureID()
        {
            if (string.IsNullOrWhiteSpace(_UniqueID))
            {
                SetFileNameAsID();
            }
        }
#endif
    }
}
