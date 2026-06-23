// Author: František Holubec

#if UNITY_EDITOR && ADDRESSABLES
using EDIVE.AddressableAssets;
using Sirenix.OdinInspector.Editor.Validation;
using UnityEditor;

[assembly: RegisterValidationRule(typeof(EDIVE.Networking.Scenes.SceneReferenceValidator))]
namespace EDIVE.Networking.Scenes
{
    public class SceneReferenceValidator : ValueValidator<SceneReference>
    {
        protected override void Validate(ValidationResult result)
        {
            var value = Value;
            if (value.Kind == SceneKind.Direct)
            {
                var asset = value.EditorDirectSceneAsset;
                if (asset == null)
                    return;

                var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
                if (string.IsNullOrEmpty(guid) || !AddressablesEditorUtils.IsAddressable(guid))
                    return;

                result.AddWarning($"Scene '{asset.name}' is Addressable but referenced as a Direct scene. Reference it as an Addressable scene instead.")
                    .WithFix(() => Value = SceneReference.FromAddressableSceneAsset(asset));
            }
        }
    }
}
#endif
