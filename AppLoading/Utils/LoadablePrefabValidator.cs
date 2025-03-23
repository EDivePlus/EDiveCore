#if UNITY_EDITOR
using EDIVE.AppLoading.Loadables;
using EDIVE.AppLoading.Utils;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions;
using Sirenix.OdinInspector.Editor.Validation;
using UnityEngine;
[assembly: RegisterValidator(typeof(LoadablePrefabValidator<>))]
namespace EDIVE.AppLoading.Utils
{
    public class LoadablePrefabValidator<TLoadable> : RootObjectValidator<TLoadable> where TLoadable : MonoBehaviour, ILoadInterface
    {
        protected override void Validate(ValidationResult result)
        {
            if (Object == null)
                return;

            var depth = CalculateDepth(Object.transform, out var topParent);
            if (depth > 0 && (!topParent.TryGetComponent<PrefabChildLoadableProvider>(out var provider) || !provider.Contains(Object.gameObject)))
            {
                result.AddError("Loadable objects in children are not supported, move it to Root or use LoadablePrefabProvider")
                    .WithFix(() =>
                    {
                        provider = topParent.GetOrAddComponent<PrefabChildLoadableProvider>();
                        provider.Add(Object.gameObject);
                        Property.ForceMarkDirty();
                    });
            }
        }

        private int CalculateDepth(Transform current, out Transform topParent)
        {
            var depth = 0;
            topParent = current;
            while (current.parent != null)
            {
                depth++;
                topParent = current = current.parent;
            }
            return depth;
        }
    }
}
#endif
