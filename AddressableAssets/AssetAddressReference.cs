using System;
using System.Linq;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Modules.Addressables.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;
#endif

namespace EDIVE.AddressableAssets
{
    [Serializable]
    [InlineProperty]
    [HideReferenceObjectPicker]
    public abstract class AssetAddressReference
#if UNITY_EDITOR
        : ISelfValidator
#endif
    {
        [CustomValueDrawer("CustomAddressDrawer")]
        [OnValueChanged("ResetEditorAsset", true)]
        [HideLabel]
        [SerializeField]
        private string _Address;

        public string Address => _Address;
        protected abstract Type ValueType { get; }

        public abstract void Release();

        public override string ToString() => $"[{_Address}]";

        [JsonConstructor]
        protected AssetAddressReference()
        {
        }

        protected AssetAddressReference(string address)
        {
            _Address = address;
        }

#if UNITY_EDITOR
        internal static readonly HashSet<AsyncOperationHandle> ACTIVE_HANDLES = new();

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            foreach (var handle in ACTIVE_HANDLES.ToList())
            {
                if (handle.IsValid())
                    handle.Release();
            }

            ACTIVE_HANDLES.Clear();
        }

        private bool _searchedForEditorAsset;
        private Object _editorAsset;

        [HideLabel]
        [EnableGUI]
        [ShowInInspector]
        [InlineIconButton("Refresh", nameof(ResetEditorAsset), GUIAlwaysEnabled  = true)]
        public Object EditorAsset
        {
            get
            {
                if (_searchedForEditorAsset)
                    return _editorAsset;

                _searchedForEditorAsset = true;
                return _editorAsset = AddressablesEditorUtils.GetAssetByAddress(Address);
            }
        }

        [OnInspectorInit]
        private void ResetEditorAsset()
        {
            _searchedForEditorAsset = false;
        }

        [UsedImplicitly]
        private string CustomAddressDrawer(string value, GUIContent label, Func<GUIContent, bool> callNextDrawer, InspectorProperty property)
        {
            var rect = EditorGUILayout.BeginHorizontal();
            callNextDrawer(label);
            var iconRect = GUILayoutUtility.GetRect(18, 18, SirenixGUIStyles.Button, GUILayoutOptions.ExpandWidth(false).Width(18));
            if (SirenixEditorGUI.IconButton(iconRect, FontAwesomeEditorIcons.SquareCaretDownRegular))
            {
                var selector = new AddressableSelector("Select", ValueType, null, typeof(AssetReference));
                selector.SelectionConfirmed += entries =>
                {
                    var entry = entries.FirstOrDefault();
                    if (entry == null) 
                        return;
                
                    _Address = entry.address;
                    _editorAsset = entry.TargetAsset;
                    _searchedForEditorAsset = true;
                    property.MarkSerializationRootDirty();
                };
                selector.ShowInPopup(rect);
            }
            EditorGUILayout.EndHorizontal();
            return value;
        }
                
        public virtual void Validate(SelfValidationResult result)
        {
            if (string.IsNullOrEmpty(Address))
                return;
            
            if (EditorAsset == null) 
                result.AddWarning("No asset found");
        }
#endif
    }

    [Serializable]
    [JsonConverter(typeof(AssetAddressReferenceJsonConverter))]
    public class AssetAddressReference<TObject> : AssetAddressReference where TObject : Object
    {
        private AsyncOperationHandle<TObject> _handle;
        
        public bool IsValid => _handle.IsValid();
        public bool IsDone => IsValid && _handle.IsDone;
        protected override Type ValueType => typeof(TObject);

        public TObject Asset => !IsDone ? null : _handle.Result;

        [JsonConstructor]
        public AssetAddressReference() { }
        public AssetAddressReference(string address) : base(address) { }

        public AsyncOperationHandle<TObject> LoadAsync()
        {
            if (string.IsNullOrEmpty(Address))
            {
                Debug.LogError("Address is null or empty");
                return default;
            }

            if (IsValid)
                return _handle;

            _handle = Addressables.LoadAssetAsync<TObject>(Address);

#if UNITY_EDITOR
            ACTIVE_HANDLES.Add(_handle);
#endif
            return _handle;
        }

        public override void Release()
        {
            if (!IsValid)
                return;

            var handle = _handle;
            Addressables.Release(handle);
            _handle = default;

#if UNITY_EDITOR
            ACTIVE_HANDLES.Remove(handle);
#endif
        }
        
#if UNITY_EDITOR
        public override void Validate(SelfValidationResult result)
        {
            base.Validate(result);
            
            if (string.IsNullOrEmpty(Address))
                return;
            
            if (EditorAsset != null && EditorAsset is not TObject) 
                result.AddError($"Asset is not of type '{typeof(TObject).Name}'");
        }
#endif
    }
}