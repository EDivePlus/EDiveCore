using System;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.StateHandling.MultiStates;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
#endif

namespace EDIVE.XRTools.Keyboard
{
    public class KeyboardKey : MonoBehaviour
    {
        [SerializeField]
        private Button _Button;

        [SerializeField]
        private KeyType _KeyType;

        [ShowIf(nameof(_KeyType), KeyType.Text)]
        [InlineIconButton(FontAwesomeEditorIconType.MagnifyingGlassSolid, "FindCharacterText")]
        [SerializeField]
        private TMP_Text _CharacterText;

        [ShowIf(nameof(_KeyType), KeyType.Text)]
        [SerializeField]
        private string _Character;

        [ShowIf(nameof(_KeyType), KeyType.Shift)]
        [SerializeField]
        [ValidateMultiState(typeof(ShiftState))]
        private AMultiState _ShiftState;

        [ShowIf(nameof(_KeyType), KeyType.Layout)]
        [SerializeField]
        [ValidateMultiState(typeof(KeyboardLayout))]
        private AMultiState _LayoutState;

        public KeyboardController Keyboard { get; private set; }
        public KeyType KeyType => _KeyType;

        private bool _initialized;
        private bool _enabledInternal;

        private float _lastClickTime;
        private float TimeSinceLastClick => Time.time - _lastClickTime;

        private const float DOUBLE_CLICK_INTERVAL = 0.5f;

        public void OnEnable()
        {
            if (_Button)
                _Button.onClick.AddListener(OnKeyPressed);

            _enabledInternal = true;
            if (_initialized)
                RegisterListeners();
        }

        public void OnDisable()
        {
            if (_Button)
                _Button.onClick.RemoveListener(OnKeyPressed);

            _enabledInternal = false;
            UnregisterListeners();
        }

        public void Initialize(KeyboardController keyboard)
        {
            Keyboard = keyboard;

            if (_initialized)
                UnregisterListeners();

            _initialized = true;
            if (_enabledInternal)
                RegisterListeners();
        }

        private void RegisterListeners()
        {
            if (_KeyType is KeyType.Text or KeyType.Shift)
            {
                Keyboard.ShiftChanged.AddListener(OnKeyboardShiftChanged);
                OnKeyboardShiftChanged(Keyboard.ShiftState);
            }


            if (_KeyType == KeyType.Layout)
            {
                Keyboard.LayoutChanged.AddListener(OnKeyboardLayoutChanged);
                OnKeyboardLayoutChanged(Keyboard.CurrentLayout);
            }
        }

        private void UnregisterListeners()
        {
            Keyboard.ShiftChanged.RemoveListener(OnKeyboardShiftChanged);
            Keyboard.LayoutChanged.RemoveListener(OnKeyboardLayoutChanged);
        }

        private void OnKeyboardShiftChanged(ShiftState shift)
        {
            if (_KeyType == KeyType.Text)
                _CharacterText.text = Keyboard.IsShifted ? _Character.ToUpper() : _Character.ToLower();

            if (_KeyType == KeyType.Shift && _ShiftState != null)
                _ShiftState.SetState(Keyboard.ShiftState);
        }

        private void OnKeyboardLayoutChanged(KeyboardLayout obj)
        {
            if (_KeyType == KeyType.Layout && _LayoutState != null)
                _LayoutState.SetState(Keyboard.CurrentLayout);
        }

        private void OnKeyPressed()
        {
            if (!_initialized)
                return;

            switch (_KeyType)
            {
                case KeyType.None:
                    break;
                case KeyType.Text:
                    Keyboard.InsertText(_CharacterText.text);
                    break;
                case KeyType.Space:
                    Keyboard.InsertText(" ");
                    break;
                case KeyType.Enter:
                    Keyboard.Enter();
                    break;
                case KeyType.Backspace:
                    Keyboard.Backspace();
                    break;
                case KeyType.Delete:
                    Keyboard.Delete();
                    break;
                case KeyType.Clear:
                    Keyboard.Clear();
                    break;
                case KeyType.Shift:
                    if (Keyboard.ShiftState != ShiftState.CapsLock && TimeSinceLastClick < DOUBLE_CLICK_INTERVAL)
                    {
                        Keyboard.Shift(ShiftState.CapsLock);
                        break;
                    }
                    Keyboard.Shift(Keyboard.IsShifted ? ShiftState.None : ShiftState.Shift);
                    break;
                case KeyType.Layout:
                    Keyboard.SetLayout(Keyboard.CurrentLayout.Next());
                    break;
                case KeyType.Hide:
                    Keyboard.Close(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Keyboard.KeyPressed.Dispatch(this);
            _lastClickTime = Time.time;
        }

#if UNITY_EDITOR
        [UsedImplicitly]
        private void FindCharacterText(InspectorProperty property)
        {
            _CharacterText = GetComponentInChildren<TMP_Text>();
            if (_KeyType == KeyType.Text && _CharacterText)
                _Character = _CharacterText.text;
            property.MarkSerializationRootDirty();
        }

        [Button(DisplayParameters = false)]
        private void RefreshName(InspectorProperty property)
        {
            gameObject.name = $"Key ({(_KeyType == KeyType.Text ? _Character : _KeyType.ToString())})";
            property.MarkSerializationRootDirty();
        }
#endif
    }
}