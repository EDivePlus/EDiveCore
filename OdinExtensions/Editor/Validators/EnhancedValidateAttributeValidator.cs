using System;
using System.Collections.Generic;
using System.Reflection;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.OdinExtensions.Editor.Validators;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ActionResolvers;
using Sirenix.OdinInspector.Editor.Validation;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;
using NamedValue = Sirenix.OdinInspector.Editor.ActionResolvers.NamedValue;

[assembly: RegisterValidator(typeof(EnhancedValidateAttributeValidator<>))]
namespace EDIVE.OdinExtensions.Editor.Validators
{
    public class EnhancedValidateAttributeValidator<T> : AttributeValidator<EnhancedValidateAttribute, T>
    {
        private static readonly NamedValue[] CUSTOM_VALIDATION_ARGS =
        {
            new("value", typeof(T)),
            new("result", typeof(SelfValidationResult))
        };
        
        private bool _isList;
        private bool _isListElement;
        
        private ActionResolver _validationChecker;

        public override RevalidationCriteria RevalidationCriteria
        {
            get
            {
                if (Attribute.ContinuousValidationCheck)
                    return RevalidationCriteria.Always;
                return Attribute.IncludeChildren ? RevalidationCriteria.OnValueChangeOrChildValueChange : RevalidationCriteria.OnValueChange;
            }
        }

        protected override void Initialize()
        {
            _isList = Property.ChildResolver is ICollectionResolver;
            _isListElement = Property.Parent != null && Property.Parent.ChildResolver is ICollectionResolver;
            
            var context = ActionResolverContext.CreateDefault(Property, Attribute.ValidationMethod, CUSTOM_VALIDATION_ARGS);
            _validationChecker = ActionResolver.GetFromContext(ref context);
        }

        protected override void Validate(ValidationResult result)
        {
            if (_isList && Attribute.ApplyToListElements)
                return;
            
            if (_isListElement && !Attribute.ApplyToListElements)
                return;
            
            if (_validationChecker.HasError)
            {
                result.AddError(ActionResolver.GetCombinedErrors(_validationChecker));
            }
            else
            {
                var selfResult = new SelfValidationResult();
                _validationChecker.Context.NamedValues.Set("value", ValueEntry.SmartValue);
                _validationChecker.Context.NamedValues.Set("result", selfResult);
                _validationChecker.DoAction();
                PopulateValidationResult(result, selfResult);
            }
        }
        
        private void PopulateValidationResult(ValidationResult result, SelfValidationResult selfResult)
        {
            var count = selfResult.Count;
            for (var i = 0; i < count; i++)
            {
                ref var selfEntry = ref selfResult[i];
                var item = new ResultItem
                {
                    Message = selfEntry.Message,
                    ResultType = selfEntry.ResultType switch
                    {
                        SelfValidationResult.ResultType.Error => ValidationResultType.Error,
                        SelfValidationResult.ResultType.Warning => ValidationResultType.Warning,
                        SelfValidationResult.ResultType.Valid => ValidationResultType.Valid,
                        _ => throw new NotImplementedException(selfEntry.ResultType.ToString())
                    }
                };

                if (selfEntry.MetaData != null)
                {
                    var metaData = new ResultItemMetaData[selfEntry.MetaData.Length];

                    for (var j = 0; j < metaData.Length; j++)
                    {
                        var metaDataItem = selfEntry.MetaData[j];
                        metaData[j] = new ResultItemMetaData(metaDataItem.Name, metaDataItem.Value, metaDataItem.Attributes);
                    }

                    item.MetaData = metaData;
                }

                if (selfEntry.Fix.HasValue)
                {
                    var fix = selfEntry.Fix.Value;

                    if (fix.Action.GetType() == typeof(Action))
                    {
                        item.Fix = new Fix(fix.Title, (Action) fix.Action, fix.OfferInInspector);
                    }
                    else if (fix.Action.GetType().IsGenericType && fix.Action.GetType().GetGenericTypeDefinition() == typeof(Action<>))
                    {
                        item.Fix = new Fix()
                        {
                            Title = fix.Title,
                            Action = fix.Action,
                            OfferInInspector = fix.OfferInInspector,
                            ArgType = fix.Action.GetType().GetGenericArguments()[0]
                        };
                    }
                    else
                    {
                        result.AddError(
                            $"Given fix '{fix.Title}' had an invalid delegate type of '{fix.Action.GetType().GetNiceName()}', for validation {selfEntry.ResultType.ToString().ToLower()} result with message '{selfEntry.Message}'; only System.Action and System.Action<T> are allowed.");
                        continue;
                    }
                }

                if (selfEntry.OnContextClick != null)
                {
                    item.OnContextClick = CreateOnContextClickInvoker(selfEntry.OnContextClick);
                }

                item.OnSceneGUI = selfEntry.OnSceneGUI;
                item.SelectionObject = selfEntry.SelectionObject;
                item.RichText = selfEntry.RichText;

                result.Add(item);
            }
        }

        private static Action<GenericMenu> CreateOnContextClickInvoker(Func<IEnumerable<SelfValidationResult.ContextMenuItem>> onContextClick)
        {
            return menu =>
            {
                foreach (var item in onContextClick())
                {
                    if (item.AddSeparatorBefore)
                    {
                        var lastForwardSlash = item.Path.LastIndexOf('/');
                        menu.AddSeparator(lastForwardSlash > 0 ? item.Path[..lastForwardSlash] : "");
                    }

                    menu.AddItem(new GUIContent(item.Path), item.On, CreateMenuFunction(item.OnClick));
                }
            };
        }

        private static GenericMenu.MenuFunction CreateMenuFunction(Action onClick)
        {
            return (GenericMenu.MenuFunction)Delegate.CreateDelegate(typeof(GenericMenu.MenuFunction), onClick.Target, onClick.Method);
        }
    }
}
