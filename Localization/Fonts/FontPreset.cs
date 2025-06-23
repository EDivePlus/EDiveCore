using System;
using System.Collections;
using System.Linq;
using EDIVE.OdinExtensions.Attributes;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

#if UNITY_EDITOR
using EDIVE.EditorUtils;
#endif

namespace EDIVE.Localization.Fonts
{
    [Serializable]
    public class FontPreset
    {
        [SerializeField]
        private TMP_FontAsset _Font;

        [SerializeField]
        [EnhancedValueDropdown("GetAllFontMaterials", AppendNextDrawer = true, DontReloadOnInit = true)]
        private Material _Material;

        public TMP_FontAsset Font => _Font;
        public Material Material => _Material;

#if UNITY_EDITOR
        [UsedImplicitly]
        private IEnumerable GetAllFontMaterials()
        {
            return _Font == null ? null : EditorAssetUtils.FindAllAssetsOfType<Material>()
                .Where(m => m.name.StartsWith(_Font.name) && m.mainTexture == _Font.atlasTexture)
                .Select(m => new ValueDropdownItem<Material>(m.name.Remove(0, _Font.name.Length).Trim(), m));
        }
#endif
    }
}
