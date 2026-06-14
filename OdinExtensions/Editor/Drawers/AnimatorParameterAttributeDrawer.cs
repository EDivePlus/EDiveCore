using System.Collections.Generic;
using System.Linq;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities.Editor;
using UnityEditor.Animations;
using UnityEngine;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    public class AnimatorParameterAttributeDrawer : OdinAttributeDrawer<AnimatorParameterAttribute, string>
    {
        private ValueResolver<object> _animatorResolver;

        protected override void Initialize()
        {
            if (!string.IsNullOrEmpty(Attribute.AnimatorGetter))
                _animatorResolver = ValueResolver.Get<object>(Property, Attribute.AnimatorGetter);
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            if (_animatorResolver != null)
                ValueResolver.DrawErrors(_animatorResolver);

            var controller = ResolveController();
            if (controller == null)
            {
                ValueEntry.SmartValue = SirenixEditorFields.TextField(label, ValueEntry.SmartValue);
                return;
            }

            var parameters = GetParameters(controller);
            var current = ValueEntry.SmartValue;
            var display = BuildDisplay(current, parameters);

            GenericSelector<AnimatorControllerParameter>.DrawSelectorDropdown(label, display, rect =>
            {
                var selector = new GenericSelector<AnimatorControllerParameter>(null, false, p => $"{p.name}", parameters);

                var selected = parameters.FirstOrDefault(p => p.name == current);
                if (selected != null)
                    selector.SetSelection(selected);

                selector.SelectionTree.Config.DrawSearchToolbar = parameters.Count > 8;
                selector.EnableSingleClickToSelect();
                selector.SelectionConfirmed += selection =>
                {
                    var pick = selection.FirstOrDefault();
                    if (pick == null) return;
                    Property.ValueEntry.WeakSmartValue = pick.name;
                    Property.MarkSerializationRootDirty();
                };
                selector.ShowInPopup(rect);
                return selector;
            });
        }

        private string BuildDisplay(string current, List<AnimatorControllerParameter> parameters)
        {
            if (string.IsNullOrEmpty(current))
                return "(None)";

            var match = parameters.FirstOrDefault(p => p.name == current);
            return match != null ? $"{current}" : $"{current}  (missing)";
        }

        private List<AnimatorControllerParameter> GetParameters(AnimatorController controller)
        {
            IEnumerable<AnimatorControllerParameter> parameters = controller.parameters;
            if (Attribute.FilterTypes?.Length > 0)
                parameters = parameters.Where(p => Attribute.FilterTypes.Contains(p.type));
            return parameters.ToList();
        }

        private AnimatorController ResolveController()
        {
            var source = _animatorResolver != null
                ? _animatorResolver.GetValue()
                : ResolveFallbackAnimator();

            return ToController(source);
        }

        private object ResolveFallbackAnimator()
        {
            var parent = Property.ParentValues.FirstOrDefault() ?? Property.Tree.WeakTargets.FirstOrDefault();
            return parent switch
            {
                Animator animator => animator,
                Component component => component.GetComponentInChildren<Animator>(),
                GameObject go => go.GetComponentInChildren<Animator>(),
                _ => null
            };
        }

        private static AnimatorController ToController(object source)
        {
            return source switch
            {
                AnimatorController controller => controller,
                Animator animator => FromRuntime(animator.runtimeAnimatorController),
                RuntimeAnimatorController runtime => FromRuntime(runtime),
                _ => null
            };
        }

        private static AnimatorController FromRuntime(RuntimeAnimatorController runtime)
        {
            return runtime switch
            {
                AnimatorController controller => controller,
                AnimatorOverrideController overrideController => FromRuntime(overrideController.runtimeAnimatorController),
                _ => null
            };
        }
    }
}
