// Author: František Holubec
// Created: 08.10.2026

using EDIVE.View.ViewTree.Editor;
using Sirenix.OdinInspector.Editor.Validation;
using UnityEngine;

[assembly: RegisterValidator(typeof(MissingViewAdapterValidator))]
namespace EDIVE.View.ViewTree.Editor
{
    public class MissingViewAdapterValidator : RootObjectValidator<Component>
    {
        protected override bool CanValidateObject(Component component)
        {
            return component != null && ViewTreeEditorUtils.AdapterTypes.ContainsKey(component.GetType());
        }

        protected override void Validate(ValidationResult result)
        {
            if (Application.isPlaying || Value == null)
                return;

            ViewTreeEditorUtils.ValidateMissingAdapter(Value, result, Property);
        }
    }
}
