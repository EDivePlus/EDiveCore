// Author: František Holubec
// Created: 2026-05-07

using EDIVE.StateHandling.ToggleStates;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

namespace EDIVE.Input.Touch
{
    public enum AxisOptions
    {
        Both, 
        Horizontal, 
        Vertical
    }

    public enum JoystickMode
    {
        [Tooltip("Stays in a fixed position")]
        Fixed, 
        [Tooltip("Starts with touch, fixed until released")]
        Floating, 
        [Tooltip("Starts with touch, moves with the touch until released")]
        Dynamic
    }
    
    public class VirtualJoystick : OnScreenControl, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [InputControl(layout = "Vector2")]
        [SerializeField]
        private string _ControlPath;
        
        [SerializeField]
        private JoystickMode _Mode = JoystickMode.Fixed;

        [MinValue(0f)]
        [SerializeField]
        [ShowIf(nameof(_Mode), JoystickMode.Dynamic)]
        [Tooltip("Magnitude past which the background follows the finger (Dynamic mode only)")]
        private float _MoveThreshold = 1f;
        
        [SerializeField]
        private AxisOptions _AxisOptions = AxisOptions.Both;

        [HideIf(nameof(_AxisOptions), AxisOptions.Vertical)]
        [SerializeField]
        private bool _SnapX;

        [HideIf(nameof(_AxisOptions), AxisOptions.Horizontal)]
        [SerializeField]
        private bool _SnapY;
        
        [MinValue(0f)]
        [SerializeField] 
        private float _HandleRange = 1f;

        [MinValue(0f)]
        [SerializeField] 
        private float _DeadZone;
        
        [Required]
        [SerializeField] 
        private RectTransform _Background;

        [Required]
        [SerializeField]
        private RectTransform _Handle;
        
        [SerializeField]
        private AToggleState _HoldingState;

        public float Horizontal => _SnapX ? SnapFloat(_input.x, AxisOptions.Horizontal) : _input.x;
        public float Vertical => _SnapY ? SnapFloat(_input.y, AxisOptions.Vertical) : _input.y;
        public Vector2 Direction => new(Horizontal, Vertical);

        public AxisOptions AxisOptions
        {
            get => _AxisOptions; 
            set => _AxisOptions = value;
        }
        public bool SnapX
        {
            get => _SnapX; 
            set => _SnapX = value;
        }
        public bool SnapY
        {
            get => _SnapY; 
            set => _SnapY = value;
        }
        public float HandleRange
        {
            get => _HandleRange; 
            set => _HandleRange = Mathf.Abs(value);
        }
        public float DeadZone
        {
            get => _DeadZone; 
            set => _DeadZone = Mathf.Abs(value);
        }
        public float MoveThreshold
        {
            get => _MoveThreshold; 
            set => _MoveThreshold = Mathf.Abs(value);
        }
        
        public JoystickMode Mode => _Mode;

        protected override string controlPathInternal
        {
            get => _ControlPath; 
            set => _ControlPath = value;
        }
        
        private static readonly Vector2 CENTER = new(0.5f, 0.5f);

        private RectTransform _baseRect;
        private Canvas _canvas;
        private Vector2 _input;
        private Vector2 _fixedBackgroundPosition;
        private int _activePointerId = -1;

        public void SetMode(JoystickMode mode)
        {
            _Mode = mode;
            if (_Background == null) 
                return;
            
            if (mode == JoystickMode.Fixed)
            {
                _Background.anchoredPosition = _fixedBackgroundPosition;
                _Background.gameObject.SetActive(true);
            }
            else
            {
                _Background.gameObject.SetActive(false);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            _baseRect = (RectTransform)transform;
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas == null)
            {
                Debug.LogError("VariableJoystick must be placed inside a Canvas.", this);
                return;
            }

            _Background.anchorMin = CENTER;
            _Background.anchorMax = CENTER;
            _Background.pivot = CENTER;
            _Handle.anchorMin = CENTER;
            _Handle.anchorMax = CENTER;
            _Handle.pivot = CENTER;
            _Handle.anchoredPosition = Vector2.zero;
            _fixedBackgroundPosition = _Background.anchoredPosition;
            SetMode(_Mode);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_activePointerId != -1) return;
            _activePointerId = eventData.pointerId;

            if (_Mode != JoystickMode.Fixed)
            {
                _Background.anchoredPosition = ScreenPointToAnchoredPosition(eventData.position, eventData.pressEventCamera);
                _Background.gameObject.SetActive(true);
            }
            UpdateInput(eventData);

            if (_HoldingState != null)
                _HoldingState.SetState(true);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != _activePointerId) return;
            UpdateInput(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _activePointerId) return;
            _activePointerId = -1;

            _input = Vector2.zero;
            _Handle.anchoredPosition = Vector2.zero;
            if (_Mode != JoystickMode.Fixed)
                _Background.gameObject.SetActive(false);

            SendValueToControl(Vector2.zero);

            if (_HoldingState != null)
                _HoldingState.SetState(false);
        }

        private void UpdateInput(PointerEventData eventData)
        {
            var cam = eventData.pressEventCamera;

            var screenPos = RectTransformUtility.WorldToScreenPoint(cam, _Background.position);
            var radius = _Background.rect.size * 0.5f;
            _input = (eventData.position - screenPos) / (radius * _canvas.scaleFactor);
            FormatInput();
            ProcessMagnitude(_input.magnitude, _input.normalized, radius);
            _Handle.anchoredPosition = _input * radius * _HandleRange;

            SendValueToControl(Direction);
        }

        private void ProcessMagnitude(float magnitude, Vector2 normalized, Vector2 radius)
        {
            if (!(magnitude > _DeadZone))
            {
                _input = Vector2.zero;
                return;
            }

            if (_Mode == JoystickMode.Dynamic && magnitude > _MoveThreshold)
            {
                var diff = normalized * ((magnitude - _MoveThreshold) * radius);
                _Background.anchoredPosition += diff;
            }

            if (magnitude > 1f)
                _input = normalized;
        }

        private void FormatInput()
        {
            _input = _AxisOptions switch
            {
                AxisOptions.Horizontal => new Vector2(_input.x, 0f),
                AxisOptions.Vertical => new Vector2(0f, _input.y),
                _ => _input
            };
        }

        private float SnapFloat(float value, AxisOptions snapAxis)
        {
            if (value == 0f) return 0f;

            if (_AxisOptions != AxisOptions.Both) 
                return value > 0f ? 1f : -1f;
            
            var angle = Vector2.Angle(_input, Vector2.up);
            return snapAxis switch
            {
                AxisOptions.Horizontal => angle is < 22.5f or > 157.5f ? 0f : (value > 0f ? 1f : -1f),
                AxisOptions.Vertical => angle is > 67.5f and < 112.5f ? 0f : (value > 0f ? 1f : -1f),
                _ => value
            };
        }

        private Vector2 ScreenPointToAnchoredPosition(Vector2 screenPosition, Camera cam)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_baseRect, screenPosition, cam, out var localPoint))
                return Vector2.zero;

            // anchoredPosition = localPoint + (baseRect.pivot - bg.anchor) * baseRect.size
            // Background anchor is enforced to CENTER in OnEnable, so this collapses cleanly.
            var size = _baseRect.rect.size;
            return localPoint + (_baseRect.pivot - CENTER) * size;
        }
    }
}
