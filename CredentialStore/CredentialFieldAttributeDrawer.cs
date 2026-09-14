// Author: František Holubec
// Created: 15.09.2026

using System;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Editor;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.CredentialStore
{
    [DrawerPriority(0, 100, 0)]
    public sealed class CredentialFieldAttributeDrawer : OdinAttributeDrawer<CredentialFieldAttribute, string>
    {
        private const string PASSWORD_PARAM = "password";

        private ValueResolver<string> _serviceResolver;
        private ValueResolver<string> _accountResolver;
        private ValueResolver<string> _validateResolver;

        private string _service;
        private string _account;
        private bool _saved;
        private CheckState _state;
        
        private string _message;
        private MessageType _messageType;
        
        private bool IsTargetValid => !string.IsNullOrEmpty(_service) && !string.IsNullOrEmpty(_account);

        protected override void Initialize()
        {
            _serviceResolver = ValueResolver.GetForString(Property, Attribute.Service);
            _accountResolver = ValueResolver.GetForString(Property, Attribute.Account);

            if (!string.IsNullOrEmpty(Attribute.ValidationMethod))
                _validateResolver = ValueResolver.Get<string>(Property, Attribute.ValidationMethod, new NamedValue(PASSWORD_PARAM, typeof(string)));
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            ValueResolver.DrawErrors(_serviceResolver, _accountResolver, _validateResolver);
            if (_serviceResolver.HasError || _accountResolver.HasError)
            {
                CallNextDrawer(label);
                return;
            }

            if (Event.current.type == EventType.Layout) 
                Rebind(_serviceResolver.GetValue(), _accountResolver.GetValue());
            
            if (!string.IsNullOrEmpty(_message))
                SirenixEditorGUI.MessageBox(_message, _messageType);
            
            var typed = ValueEntry.SmartValue;
            SirenixEditorGUI.BeginHorizontalPropertyLayout(label);

            EditorGUI.BeginChangeCheck();
            var edited = EditorGUILayout.PasswordField(typed);
            if (EditorGUI.EndChangeCheck())
            {
                ValueEntry.SmartValue = edited;
                SetState(CheckState.Unchecked);
                ClearMessage();
            }

            if (_validateResolver != null && !_validateResolver.HasError)
            {
                GetStateVisual(_state, out var stateColor, out var stateIcon, out var stateTooltip);
                GUIHelper.PushColor(stateColor);
                if (DrawButton(stateIcon, stateTooltip))
                    Validate();
                GUIHelper.PopColor();
                GUILayout.Space(4);
            }
            
            var hasTyped = !string.IsNullOrEmpty(typed);
            if (DrawButton(FontAwesomeEditorIcons.FloppyDiskSolid, "Save", hasTyped))
                Save();
            
            if (DrawButton(FontAwesomeEditorIcons.TrashSolid, "Clear", _saved))
                Clear();

            SirenixEditorGUI.EndHorizontalPropertyLayout();
        }

        private bool DrawButton(EditorIcon icon, string tooltip, bool enabled = true)
        {
            var iconRect = GUILayoutUtility.GetRect(18, 18, SirenixGUIStyles.Button,  GUILayoutOptions.ExpandWidth(false).Width(18));
            GUIHelper.PushGUIEnabled(enabled);
            var result = SirenixEditorGUI.IconButton(iconRect, icon, tooltip);
            GUIHelper.PopGUIEnabled();
            return result;
        }
        
        private void Rebind(string service, string account)
        {
            if (service == _service && account == _account)
                return;

            _service = service;
            _account = account;
            // An empty service or account is a field that is not configured yet, not a store failure.
            _saved = IsTargetValid && Credentials.Contains(service, account);
            SetState(CheckState.Unchecked);
            ClearMessage();
        }

        private void Save()
        {
            var result = Credentials.Set(_service, _account, ValueEntry.SmartValue);
            if (!result)
            {
                SetMessage($"Could not save. {result}", MessageType.Error);
                return;
            }
            
            _saved = true;
            ClearMessage();
        }

        private void Clear()
        {
            if (IsTargetValid)
                Credentials.Delete(_service, _account);

            ValueEntry.SmartValue = null;
            _saved = false;
            SetState(CheckState.Unchecked);
            ClearMessage();
        }

        private void Validate()
        {
            if (_validateResolver == null || _validateResolver.HasError)
                return;

            var password = ValueEntry.SmartValue;
            if (string.IsNullOrEmpty(password) && IsTargetValid)
            {
                Credentials.Get(_service, _account, out password);
            }
            
            if (string.IsNullOrEmpty(password))
            {
                SetState(CheckState.Unchecked);
                SetMessage("No password to check", MessageType.Warning);
                return;
            }

            _validateResolver.Context.NamedValues.Set(PASSWORD_PARAM, password);
            var error = _validateResolver.GetValue();
            SetState(string.IsNullOrEmpty(error) ? CheckState.Valid : CheckState.Invalid);
            if (string.IsNullOrEmpty(error))
                ClearMessage();
            else
                SetMessage(error, MessageType.Error);
        }

        private void SetState(CheckState state)
        {
            _state = state;
        }

        private void SetMessage(string message, MessageType messageType)
        {
            _message = message;
            _messageType = messageType;
        }

        private void ClearMessage() => _message = null;
        
        private void GetStateVisual(CheckState state, out Color color, out EditorIcon icon, out string tooltip)
        {
            switch (state)
            {
                case CheckState.Unchecked:
                    color = Color.white;
                    icon = FontAwesomeEditorIcons.ShieldSolid;
                    tooltip = "Unchecked, click to check";
                    break;
                case CheckState.Valid:
                    color = Color.greenYellow;
                    icon = FontAwesomeEditorIcons.ShieldCheckSolid;
                    tooltip = "Valid";
                    break;
                case CheckState.Invalid:
                    color = Color.orangeRed;
                    icon = FontAwesomeEditorIcons.ShieldXmarkSolid;
                    tooltip = "Invalid";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        private enum CheckState
        {
            Unchecked,
            Valid,
            Invalid
        }
    }
}
