using System.Collections.Generic;
using System.Linq;
using EDIVE.AssetTranslation;
using EDIVE.NativeUtils;
using EDIVE.VisualPresets.Presets;
using EDIVE.VisualPresets.Switchers;
using EDIVE.VisualPresets.VisualIDs;
using JetBrains.Annotations;
using PurrNet.Packing;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using EDIVE.EditorUtils;
#endif

namespace EDIVE.Avatars
{
    public class AvatarDefinition : AUniqueDefinition
    {
        [SerializeField]
        private AvatarController _AvatarPrefab;

        [SerializeField]
        private VisualPreset _Visual;
        
        [SerializeField]
        [ListDrawerSettings(OnTitleBarGUI = "DrawVisualIDsTitleBar")]
        private List<ABaseVisualID> _CustomizationIDs = new();

        public AvatarController AvatarPrefab => _AvatarPrefab;
        public VisualPreset Visual => _Visual;
        public IReadOnlyList<ABaseVisualID> CustomizationIDs => _CustomizationIDs;

        public bool IsValid() => _AvatarPrefab != null;
        
#if UNITY_EDITOR
        [UsedImplicitly]
        private void DrawVisualIDsTitleBar(InspectorProperty property)
        {
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
            {
                if (_AvatarPrefab == null)
                    return;

                SerializedObjectUtils.GetSerializedPropertiesOfType<VisualSwitcher>(_AvatarPrefab)
                    .SelectMany(s => s.GetVisualIDs())
                    .Where(id => id != null)
                    .Distinct()
                    .ForEach(id =>
                    {
                        if (!_CustomizationIDs.Contains(id))
                            _CustomizationIDs.Add(id);
                    });

                property.MarkSerializationRootDirty();
            }
        }
#endif
    }

    // Used by PurrNet for serialization of AvatarDefinition references.
    // PurrNet auto-discovers static classes that contain `Write(this BitPacker, T)` and
    // `Read(this BitPacker, ref T)` extension method pairs.
    [UsedImplicitly]
    public static class AvatarDefinitionExtensions
    {
        public static void Write(this BitPacker packer, AvatarDefinition value) => packer.CustomWriteTranslatedDefinition(value);
        public static void Read(this BitPacker packer, ref AvatarDefinition value) => value = packer.CustomReadTranslatedDefinition<AvatarDefinition>();
    }
}
