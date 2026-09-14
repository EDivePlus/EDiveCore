// Author: František Holubec
// Created: 14.09.2026

using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.CredentialStore
{
    public class CredentialStoreEditorWindow : OdinEditorWindow
    {
        private const string TITLE = "Credential Store";

        [PropertyOrder(-10)]
        [ShowInInspector]
        [ReadOnly]
        [LabelText("Store")]
        private string StoreName => _storeStatus ??= DescribeStore();

        [NonSerialized]
        private string _storeStatus;

        private static string DescribeStore()
        {
            var availability = Credentials.CheckAvailability();
            return availability ? Credentials.Store.Name : $"{Credentials.Store.Name} — {availability}";
        }

        [TitleGroup("Add")]
        [ShowInInspector]
        [NonSerialized]
        [LabelText("Service")]
        private string _newService = string.Empty;

        [TitleGroup("Add")]
        [ShowInInspector]
        [NonSerialized]
        [LabelText("Account")]
        private string _newAccount;

        [TitleGroup("Add")]
        [ShowInInspector]
        [NonSerialized]
        [PasswordField]
        [LabelText("Secret")]
        private string _newSecret;

        [TitleGroup("Stored")]
        [ShowInInspector]
        [NonSerialized]
        [HideLabel]
        [InfoBox("$" + nameof(_listError), InfoMessageType.Warning, nameof(HasListError))]
        [TableList(IsReadOnly = true, AlwaysExpanded = true)]
        private List<CredentialRow> _rows = new();

        [NonSerialized]
        private string _listError;

        private bool _refreshPending;

        [MenuItem("Tools/Credential Store")]
        private static void Open()
        {
            var window = GetWindow<CredentialStoreEditorWindow>();
            window.titleContent = new GUIContent(TITLE);
            window.Show();
        }

        protected override void Initialize()
        {
            base.Initialize();
            Refresh();
        }

        protected override void OnImGUI()
        {
            if (_refreshPending)
            {
                _refreshPending = false;
                Refresh();
            }
            base.OnImGUI();
        }

        private bool CanAdd => !string.IsNullOrEmpty(_newService) && !string.IsNullOrEmpty(_newAccount) && _newSecret != null;
        private bool HasListError => _listError != null;

        [TitleGroup("Add")]
        [Button("Save")]
        [EnableIf(nameof(CanAdd))]
        private void AddCredential()
        {
            if (!Check(Credentials.Set(_newService, _newAccount, _newSecret)))
                return;

            _newAccount = null;
            _newSecret = null;
            GUIUtility.keyboardControl = 0;
            RequestRefresh();
        }

        [TitleGroup("Stored")]
        [Button("Refresh")]
        private void RequestRefresh()
        {
            _refreshPending = true;
            Repaint();
        }

        private void Refresh()
        {
            _storeStatus = null;
            var result = Credentials.List(out var entries);
            _listError = result ? null : result.ToString();
            _rows = entries
                .OrderBy(e => e.Service, StringComparer.OrdinalIgnoreCase)
                .ThenBy(e => e.Account, StringComparer.OrdinalIgnoreCase)
                .Select(e => new CredentialRow(this, e))
                .ToList();
        }

        private static bool Check(CredentialResult result)
        {
            if (!result)
                EditorUtility.DisplayDialog(TITLE, result.ToString(), "OK");
            return result;
        }

        private class CredentialRow
        {
            private readonly CredentialStoreEditorWindow _editorWindow;
            private readonly CredentialEntry _entry;

            public CredentialRow(CredentialStoreEditorWindow editorWindow, CredentialEntry entry)
            {
                _editorWindow = editorWindow;
                _entry = entry;
            }

            [ShowInInspector]
            [ReadOnly]
            [TableColumnWidth(140)]
            private string Service => _entry.Service;

            [ShowInInspector]
            [ReadOnly]
            [TableColumnWidth(140)]
            private string Account => _entry.Account;

            [ShowInInspector]
            [PasswordField]
            [TableColumnWidth(140)]
            [LabelText("New Secret")]
            private string NewSecret { get; set; }

            private bool CanSave => NewSecret != null;

            [TableColumnWidth(170, false)]
            [HorizontalGroup("Actions")]
            [Button("Save")]
            [EnableIf(nameof(CanSave))]
            private void Save()
            {
                if (!Check(Credentials.Set(_entry.Service, _entry.Account, NewSecret)))
                    return;

                NewSecret = null;
                GUIUtility.keyboardControl = 0;
                _editorWindow.RequestRefresh();
            }

            [HorizontalGroup("Actions")]
            [Button("Copy")]
            private void Copy()
            {
                if (!Check(Credentials.Get(_entry.Service, _entry.Account, out var secret)))
                    return;

                EditorGUIUtility.systemCopyBuffer = secret;
                _editorWindow.ShowNotification(new GUIContent("Copied"));
            }

            [HorizontalGroup("Actions")]
            [Button("Delete")]
            private void Delete()
            {
                if (!EditorUtility.DisplayDialog(TITLE, $"Delete {_entry}?", "Delete", "Cancel"))
                    return;

                var result = Credentials.Delete(_entry.Service, _entry.Account);
                if (result.Status != CredentialStatus.NotFound)
                    Check(result);
                _editorWindow.RequestRefresh();
            }
        }
    }
}
