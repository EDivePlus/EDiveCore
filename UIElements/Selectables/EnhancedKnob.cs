using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

#if UNITY_EDITOR
using EDIVE.OdinExtensions.Editor;
#endif

namespace EDIVE.UIElements.Selectables
{
    public class EnhancedKnob : Selectable, IDragHandler, IInitializePotentialDragHandler
    {
        public enum Direction
        {
            Clockwise,
            Counterclockwise
        }
        
        [SerializeField]
        private Direction _Direction = Direction.Clockwise;
        
        [Tooltip("If enabled, the knob output is clamped to [0, Max Value]. With loops, Max Value can exceed 1 (e.g. 2.5 = 2.5 full rotations).")]
        [SerializeField]
        private bool _ClampValue;

        [ShowIf(nameof(_ClampValue))]
        [Tooltip("Maximum RAW output value the knob can reach (cumulative when loops are enabled)")]
        [SerializeField]
        private float _MaxValue = 1f;

        [Tooltip("If enabled, the knob tracks full rotations and the output value accumulates across them")]
        [SerializeField]
        private bool _UseLoops;

        [Tooltip("snap to position?")]
        [SerializeField]
        private bool _SnapToPosition;

        [Tooltip("Number of positions to snap")]
        [ShowIf(nameof(_SnapToPosition))]
        [SerializeField]
        private int _SnapSteps = 8;

        [Tooltip("Object that visually rotates with the knob. If not set, this transform is used.")]
        [SerializeField]
        private Transform _RotationTarget;

        [PropertyRange(0, 1)]
        [OnValueChanged(nameof(ApplyValue))]
        [SerializeField]
        private float _Value;
        
        [PropertySpace]
        [SerializeField]
        private KnobFloatValueEvent _ValueChanged;
        
        [HideLabel]
        [InlineProperty]
        [SerializeField]
        private SelectableAdditionalData _AdditionalData = new();

        private float _previousValue;
        private float _initAngle;
        private Quaternion _initRotation;

        private Transform RotationTarget => _RotationTarget != null ? _RotationTarget : transform;

        public KnobFloatValueEvent ValueChanged => _ValueChanged;

        [PropertySpace]
        [ShowInInspector, ReadOnly]
        public int LoopCount { get; private set; }
        
        [ShowInInspector, ReadOnly]
        public float RawValue => _UseLoops ? _Value + LoopCount : _Value;
        
        [ShowInInspector, ReadOnly]
        public float NormalizedValue => _ClampValue && _MaxValue > 0 ? RawValue / _MaxValue : RawValue;

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            _AdditionalData.DoStateTransition((Selectables.SelectionState) state, instant);
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);

            _initRotation = RotationTarget.localRotation;
            var currentVector = GetPointerVector(eventData);
            _initAngle = Mathf.Atan2(currentVector.y, currentVector.x) * Mathf.Rad2Deg;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsInteractable())
                return;

            var currentVector = GetPointerVector(eventData);
            var currentAngle = Mathf.Atan2(currentVector.y, currentVector.x) * Mathf.Rad2Deg;

            var addRotation = Quaternion.Euler(0, 0, currentAngle - _initAngle);
            var finalRotation = _initRotation * addRotation;

            if (_Direction == Direction.Clockwise)
            {
                _Value = 1 - (finalRotation.eulerAngles.z / 360f);

                if (_SnapToPosition)
                {
                    SnapToPositionValue(ref _Value);
                    finalRotation.eulerAngles = new Vector3(0, 0, 360 - 360 * _Value);
                }
            }
            else
            {
                _Value = (finalRotation.eulerAngles.z / 360f);

                if (_SnapToPosition)
                {
                    SnapToPositionValue(ref _Value);
                    finalRotation.eulerAngles = new Vector3(0, 0, 360 * _Value);
                }
            }

            var clamped = UpdateKnobValue();

            if (clamped)
                ApplyVisualRotation();
            else
                RotationTarget.localRotation = finalRotation;
            InvokeEvents(RawValue);

            _previousValue = _Value;
        }

        private Vector2 GetPointerVector(PointerEventData eventData)
        {
            if (RotationTarget.parent is RectTransform parentRect &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventData.pressEventCamera, out var localPoint))
            {
                return localPoint - (Vector2) RotationTarget.localPosition;
            }

            var screenPoint = RectTransformUtility.WorldToScreenPoint(eventData.pressEventCamera, RotationTarget.position);
            return eventData.position - screenPoint;
        }

        private bool UpdateKnobValue()
        {
            if (_UseLoops && Mathf.Abs(_Value - _previousValue) > 0.5f)
            {
                if (_Value < 0.5f)
                    LoopCount++;
                else
                    LoopCount--;
            }

            if (!_ClampValue)
                return false;

            var raw = _UseLoops ? _Value + LoopCount : _Value;
            if (raw < 0)
            {
                _Value = 0;
                LoopCount = 0;
                return true;
            }
            if (raw > _MaxValue)
            {
                if (_UseLoops)
                {
                    LoopCount = Mathf.FloorToInt(_MaxValue);
                    _Value = Mathf.Max(0f, _MaxValue - LoopCount);
                }
                else
                {
                    _Value = _MaxValue;
                }
                return true;
            }
            return false;
        }

        private void ApplyVisualRotation()
        {
            var z = _Direction == Direction.Clockwise ? 360f - 360f * _Value : 360f * _Value;
            RotationTarget.localEulerAngles = new Vector3(0, 0, z);
        }

        public float KnobValue
        {
            get => _Value;
            set => SetValue(value);
        }

        private void ApplyValue() => SetValue(_Value, LoopCount);

        public void SetValue(float value, int loops = 0)
        {
            var newRotation = Quaternion.identity;
            _Value = value;
            LoopCount = _UseLoops ? loops : 0;

            if (_SnapToPosition)
                SnapToPositionValue(ref _Value);

            var clamped = UpdateKnobValue();

            if (clamped)
            {
                ApplyVisualRotation();
            }
            else
            {
                newRotation.eulerAngles = _Direction == Direction.Clockwise ? new Vector3(0, 0, 360 - 360 * _Value) : new Vector3(0, 0, 360 * _Value);
                RotationTarget.localRotation = newRotation;
            }
            InvokeEvents(RawValue);

            _previousValue = _Value;
        }

        private void SnapToPositionValue(ref float knobValue)
        {
            var snapStep = 1 / (float)_SnapSteps;
            var newValue = Mathf.Round(knobValue / snapStep) * snapStep;
            knobValue = newValue;
        }
        private void InvokeEvents(float value)
        {
            _ValueChanged?.Invoke(value);
        }

        public virtual void OnInitializePotentialDrag(PointerEventData eventData)
        {
            eventData.useDragThreshold = false;
        }
    }
    
    [System.Serializable]
    public class KnobFloatValueEvent : UnityEvent<float> { }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(EnhancedKnob))]
    [UnityEditor.CanEditMultipleObjects]
    public class EnhancedKnobEditor : NativeWrapperOdinEditor<Selectable, NativeSelectableEditor> { }
#endif
}
