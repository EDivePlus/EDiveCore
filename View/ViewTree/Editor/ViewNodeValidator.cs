// Author: František Holubec
// Created: 08.10.2026

using EDIVE.View.ViewTree.Editor;
using Sirenix.OdinInspector.Editor.Validation;
using UnityEngine;

[assembly: RegisterValidator(typeof(ViewNodeValidator<>))]
namespace EDIVE.View.ViewTree.Editor
{
    public class ViewNodeValidator<TNode> : RootObjectValidator<TNode> where TNode : AViewNode
    {
        protected override void Validate(ValidationResult result)
        {
            if (Application.isPlaying || Value == null)
                return;

            ViewTreeEditorUtils.Validate(Value, result, Property);
        }
    }
}
