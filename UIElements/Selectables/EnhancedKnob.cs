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
        
        [FormerlySerializedAs("direction")]
        [SerializeField]
        private Direction _Direction = Direction.Clockwise;
        
        [Tooltip("Max value of the knob, maximum RAW output value knob can reach, overrides snap step, IF set to 0 or higher than loops, max value will be set by loops")]
        [SerializeField]
        private float _MaxValue;

        [Tooltip("How many rotations knob can do, if higher than max value, the latter will limit max value")]
        [SerializeField]
        private int _Loops;
 
        [Tooltip("Clamp output value between 0 and 1, useful with loops > 1")]
        [SerializeField]
        private bool _ClampOutput01;

        [Tooltip("snap to position?")]
        [SerializeField]
        private bool _SnapToPosition;

        [Tooltip("Number of positions to snap")]
        [SerializeField]
        private int _SnapStepsPerLoop = 10;

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
        

        
        private float _currentLoops;
        private float _previousValue;
        private float _initAngle;
        private Quaternion _initRotation;

        private Transform RotationTarget => _RotationTarget != null ? _RotationTarget : transform;

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            _AdditionalData.DoStateTransition((Selectables.SelectionState) state, instant);
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);

            _initRotation = RotationTarget.rotation;
            var currentVector = GetPointerVector(eventData);
            _initAngle = Mathf.Atan2(currentVector.y, currentVector.x) * Mathf.Rad2Deg;
        }

        public void OnDrag(PointerEventData eventData)
        {
            var currentVector = GetPointerVector(eventData);
            var currentAngle = Mathf.Atan2(currentVector.y, currentVector.x) * Mathf.Rad2Deg;

            var addRotation = Quaternion.AngleAxis(currentAngle - _initAngle, RotationTarget.forward);
            addRotation.eulerAngles = new Vector3(0, 0, addRotation.eulerAngles.z);

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

            UpdateKnobValue();

            RotationTarget.rotation = finalRotation;
            InvokeEvents(_Value + _currentLoops);

            _previousValue = _Value;
        }

        private Vector2 GetPointerVector(PointerEventData eventData)
        {
            var screenPoint = RectTransformUtility.WorldToScreenPoint(eventData.pressEventCamera, RotationTarget.position);
            return eventData.position - screenPoint;
        }

        private void UpdateKnobValue()
        {
            if (Mathf.Abs(_Value - _previousValue) > 0.5f)
            {
                if (_Value < 0.5f && _Loops > 1 && _currentLoops < _Loops - 1)
                {
                    _currentLoops++;
                }
                else if (_Value > 0.5f && _currentLoops >= 1)
                {
                    _currentLoops--;
                }
                else
                {
                    if (_Value > 0.5f && _currentLoops == 0)
                    {
                        _Value = 0;
                        RotationTarget.localEulerAngles = Vector3.zero;
                        InvokeEvents(_Value + _currentLoops);
                        return;
                    }

                    if (_Value < 0.5f && Mathf.Approximately(_currentLoops, _Loops - 1))
                    {
                        _Value = 1;
                        RotationTarget.localEulerAngles = Vector3.zero;
                        InvokeEvents(_Value + _currentLoops);
                        return;
                    }
                }
            }
            
            if (_MaxValue > 0 && _Value + _currentLoops > _MaxValue)
            {
                _Value = _MaxValue;
                var maxAngle = _Direction == Direction.Clockwise ? 360f - 360f * _MaxValue : 360f * _MaxValue;
                RotationTarget.localEulerAngles = new Vector3(0, 0, maxAngle);
                InvokeEvents(_Value);
            }
        }

        public float KnobValue
        {
            get => _Value;
            set => SetValue(value);
        }

        private void ApplyValue() => SetValue(_Value, Mathf.RoundToInt(_currentLoops));

        public void SetValue(float value, int loops = 0)
        {
            var newRotation = Quaternion.identity;
            _Value = value;
            _currentLoops = loops;

            if (_SnapToPosition) 
                SnapToPositionValue(ref _Value);
            
            newRotation.eulerAngles = _Direction == Direction.Clockwise ? new Vector3(0, 0, 360 - 360 * _Value) : new Vector3(0, 0, 360 * _Value);

            UpdateKnobValue();

            RotationTarget.rotation = newRotation;
            InvokeEvents(_Value + _currentLoops);

            _previousValue = _Value;
        }

        private void SnapToPositionValue(ref float knobValue)
        {
            var snapStep = 1 / (float)_SnapStepsPerLoop;
            var newValue = Mathf.Round(knobValue / snapStep) * snapStep;
            knobValue = newValue;
        }
        private void InvokeEvents(float value)
        {
            if (_ClampOutput01 && _Loops > 0)
                value /= _Loops;
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
