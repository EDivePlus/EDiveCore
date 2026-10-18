using System;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.StateHandling.MultiStates;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
#endif

namespace EDIVE.Input.Keyboard
{
    public class VirtualKeyboardKey : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
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

        public VirtualKeyboardController Keyboard { get; private set; }
        public KeyType KeyType => _KeyType;

        private bool _initialized;
        private bool _enabledInternal;

        private float _lastClickTime;
        private float TimeSinceLastClick => UnityEngine.Time.time - _lastClickTime;

        private bool _holding;
        private bool _repeated;
        private float _nextRepeatTime;
        private PointerEventData _pressEventData;
        private bool RepeatsOnHold => _KeyType == KeyType.Backspace;

        private const float DOUBLE_CLICK_INTERVAL = 0.5f;
        private const float REPEAT_DELAY = 0.5f;
        private const float REPEAT_INTERVAL = 0.07f;

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
            _holding = false;
            if (_initialized)
                UnregisterListeners();
        }

        private void Update()
        {
            if (!_holding || UnityEngine.Time.unscaledTime < _nextRepeatTime)
                return;

            _repeated = true;
            _nextRepeatTime += REPEAT_INTERVAL;
            Keyboard.Backspace();
            Keyboard.RaiseKeyDown(this, _pressEventData);
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            if (!_initialized || !_Button || !_Button.IsInteractable())
                return;

            _pressEventData = eventData;
            Keyboard.RaiseKeyDown(this, eventData);

            if (!RepeatsOnHold)
                return;

            _holding = true;
            _repeated = false;
            _nextRepeatTime = UnityEngine.Time.unscaledTime + REPEAT_DELAY;
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData) => _holding = false;
        void IPointerExitHandler.OnPointerExit(PointerEventData eventData) => _holding = false;

        public void Initialize(VirtualKeyboardController keyboard)
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
                Keyboard.ShiftChanged += OnKeyboardShiftChanged;
                OnKeyboardShiftChanged(Keyboard.ShiftState);
            }


            if (_KeyType == KeyType.Layout)
            {
                Keyboard.LayoutChanged += OnKeyboardLayoutChanged;
                OnKeyboardLayoutChanged(Keyboard.CurrentLayout);
            }
        }

        private void UnregisterListeners()
        {
            Keyboard.ShiftChanged -= OnKeyboardShiftChanged;
            Keyboard.LayoutChanged -= OnKeyboardLayoutChanged;
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

            // The hold already deleted, the click on release must not delete once more
            if (_repeated)
            {
                _repeated = false;
                return;
            }

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
                // Handled through KeyPressed until speech to text exists
                case KeyType.Mic:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Keyboard.RaiseKeyPressed(this);
            _lastClickTime = UnityEngine.Time.time;
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
