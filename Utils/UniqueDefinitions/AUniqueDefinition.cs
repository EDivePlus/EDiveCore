using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace EDIVE.Utils.UniqueDefinitions
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class AUniqueDefinition : ScriptableObject, IUniqueDefinition
    {
        [Required]
        [PropertyOrder(-100)]
        [InlineIconButton(FontAwesomeEditorIconType.FilePenSolid, "SetFileNameAsID", "Use filename as ID")]
        [SerializeField]
        protected string _UniqueID;

        public string UniqueID => _UniqueID;

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
