// Author: František Holubec
// Created: 14.09.2026

using System.Collections.Generic;
using System.IO;
using EDIVE.BuildTool.Signing;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.BuildTool.Utils
{
    internal class AndroidKeystoreDialog : OdinEditorWindow
    {
        [PropertyOrder(0)]
        [Sirenix.OdinInspector.FilePath(AbsolutePath = true, Extensions = "keystore,jks,ks")]
        [ShowInInspector]
        private string KeystorePath
        {
            get => _keystorePath;
            set
            {
                _keystorePath = value;
                InvalidateKeystore();
            }
        }

        [PropertyOrder(1)]
        [LabelText("Store Password")]
        [EnableIf(nameof(HasKeystoreFile))]
        [PasswordField]
        [InlineIconButton(FontAwesomeEditorIconType.ArrowsRotateSolid, nameof(RefreshAliases), "Read the aliases from the keystore", EnableIf = nameof(CanReadAliases))]
        [ShowInInspector]
        private string StorePassword
        {
            get => _storePassword;
            set
            {
                _storePassword = value;
                InvalidateKeystore();
            }
        }

        [PropertyOrder(2)]
        [InfoBox("$_aliasError", InfoMessageType.Warning, VisibleIf = "@!string.IsNullOrEmpty(_aliasError)")]
        [EnhancedValueDropdown(nameof(GetAliases), AppendNextDrawer = true)]
        [EnableIf(nameof(HasKeystoreFile))]
        [ShowInInspector]
        private string KeyAlias
        {
            get => _keyAlias;
            set => _keyAlias = value;
        }

        [PropertyOrder(3)]
        [LabelText("Key Password")]
        [EnableIf(nameof(HasAlias))]
        [PasswordField]
        [ShowInInspector]
        private string KeyPassword
        {
            get => _keyPassword;
            set => _keyPassword = value;
        }

        [PropertyOrder(4)]
        [ShowInInspector]
        [Tooltip("Saves both passwords to the Credential Store so the next build runs without asking.")]
        private bool Remember
        {
            get => _remember;
            set => _remember = value;
        }

        private AndroidSigningSettings _settings;
        private AndroidSigningData _result;
        private string _keystorePath;
        private string _storePassword;
        private string _keyAlias;
        private string _keyPassword;
        private bool _remember = true;
        private string _error;
        private string _aliasError;
        private List<string> _aliasCache;
        private bool _confirmed;

        public static bool TryPrompt(string reason, AndroidSigningSettings settings, out AndroidSigningData result)
        {
            var window = CreateInstance<AndroidKeystoreDialog>();
            window.titleContent = new GUIContent("Android Keystore");
            window._error = reason;
            window._settings = settings;
            window._keystorePath = settings?.KeystorePath;
            window._keyAlias = settings?.KeyAlias;
            var size = new Vector2(460, 260);
            window.minSize = window.maxSize = size;
            window.position = CenterOnEditor(size);
            window.ShowModalUtility();

            result = window._result;
            return window._confirmed;
        }

        private bool HasKeystoreFile => !string.IsNullOrEmpty(_keystorePath) && File.Exists(_keystorePath);

        private bool HasAlias => HasKeystoreFile && !string.IsNullOrEmpty(_keyAlias);

        private bool CanReadAliases => HasKeystoreFile && !string.IsNullOrEmpty(_storePassword);

        [PropertyOrder(10)]
        [ButtonGroup]
        [Button("Cancel")]
        private void Cancel() => Close();

        [PropertyOrder(10)]
        [ButtonGroup]
        [Button("Save")]
        private void Save()
        {
            var data = new AndroidSigningData(_keystorePath, _keyAlias, _storePassword, _keyPassword);
            if (!Verify(data))
                return;

            if (_remember)
            {
                if (!AndroidSigningSettings.TrySavePasswords(_keystorePath, _keyAlias, _storePassword, _keyPassword, out var storeError))
                    Debug.LogWarning($"[Signing] Passwords were not saved. {storeError}");
                _settings?.Invalidate();
            }

            _result = data;
            _confirmed = true;
            Close();
        }

        [PropertyOrder(-100)]
        [OnInspectorGUI]
        private void DrawError()
        {
            if (!string.IsNullOrEmpty(_error))
                EditorGUILayout.HelpBox(_error, MessageType.Error);
        }

        private static Rect CenterOnEditor(Vector2 size)
        {
            var main = EditorGUIUtility.GetMainWindowPosition();
            return new Rect(main.center.x - size.x * 0.5f, main.center.y - size.y * 0.5f, size.x, size.y);
        }

        private IEnumerable<string> GetAliases() => _aliasCache ??= ReadAliases();

        private void RefreshAliases() => _aliasCache = ReadAliases();

        private List<string> ReadAliases()
        {
            _aliasError = null;
            if (!CanReadAliases)
                return new List<string>();

            return KeystoreAliasReader.TryReadAliases(_keystorePath, _storePassword, out var aliases, out _aliasError)
                ? aliases
                : new List<string>();
        }

        private void InvalidateKeystore()
        {
            _aliasCache = null;
            _aliasError = null;
        }

        private bool Verify(AndroidSigningData data)
        {
            _error = null;
            if (string.IsNullOrEmpty(data.KeystorePath) || !File.Exists(data.KeystorePath))
                _error = "Keystore file not found";
            else if (string.IsNullOrEmpty(data.StorePassword) || string.IsNullOrEmpty(data.KeyPassword))
                _error = "Both passwords are required";
            else if (string.IsNullOrEmpty(data.KeyAlias))
                _error = "No key alias is set";
            else if (!data.TryVerify(out var verifyError))
                _error = verifyError;

            return _error == null;
        }
    }
}
