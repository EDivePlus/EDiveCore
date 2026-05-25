// Based on UI_Knob from UIExtensions https://github.com/Unity-UI-Extensions/com.unity.uiextensions

using EDIVE.OdinExtensions.Editor;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace EDIVE.UIElements.Selectables
{
    public class EnhancedKnob : Selectable, IDragHandler, IInitializePotentialDragHandler
    {
        public enum Direction
        {
            Clockwise,
            Counterclockwise
        }
        
        [HideLabel]
        [InlineProperty]
        [SerializeField]
        private SelectableAdditionalData _AdditionalData = new();

        [FormerlySerializedAs("direction")]
        [SerializeField]
        private Direction _Direction = Direction.Clockwise;
        
        [Tooltip("Max value of the knob, maximum RAW output value knob can reach, overrides snap step, IF set to 0 or higher than loops, max value will be set by loops")]
        [SerializeField]
        private float _MaxValue = 0;

        [Tooltip("How many rotations knob can do, if higher than max value, the latter will limit max value")]
        [SerializeField]
        private int _Loops = 0;
 
        [Tooltip("Clamp output value between 0 and 1, useful with loops > 1")]
        [SerializeField]
        private bool _ClampOutput01 = false;

        [Tooltip("snap to position?")]
        [SerializeField]
        private bool _SnapToPosition = false;

        [Tooltip("Number of positions to snap")]
        [SerializeField]
        private int _SnapStepsPerLoop = 10;
        
        [PropertySpace]
        [SerializeField]
        private KnobFloatValueEvent _ValueChanged;
        
        [HideInInspector]
        [SerializeField]
        private float _KnobValue;
        
        private float _currentLoops = 0;
        private float _previousValue = 0;
        private float _initAngle;
        private float _currentAngle;
        private Vector2 _currentVector;
        private Quaternion _initRotation; 
        private bool _screenSpaceOverlay;

        protected override void Awake()
        {
            _screenSpaceOverlay = GetComponentInParent<Canvas>().rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay;
        }
        
        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            _AdditionalData.DoStateTransition((Selectables.SelectionState) state, instant);
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);

            _initRotation = transform.rotation;
			if (_screenSpaceOverlay)
            {
				_currentVector = eventData.position - (Vector2)transform.position;
            }
            else
            {
				_currentVector = eventData.position - (Vector2)Camera.main.WorldToScreenPoint(transform.position);
            }
            _initAngle = Mathf.Atan2(_currentVector.y, _currentVector.x) * Mathf.Rad2Deg;
        }

        public void OnDrag(PointerEventData eventData)
        {
			if (_screenSpaceOverlay)
			{
				_currentVector = eventData.position - (Vector2)transform.position;
			}
			else
			{
				_currentVector = eventData.position - (Vector2)Camera.main.WorldToScreenPoint(transform.position);
			}
            _currentAngle = Mathf.Atan2(_currentVector.y, _currentVector.x) * Mathf.Rad2Deg;

            var addRotation = Quaternion.AngleAxis(_currentAngle - _initAngle, this.transform.forward);
            addRotation.eulerAngles = new Vector3(0, 0, addRotation.eulerAngles.z);

            var finalRotation = _initRotation * addRotation;

            if (_Direction == Direction.Clockwise)
            {
                _KnobValue = 1 - (finalRotation.eulerAngles.z / 360f);

                if (_SnapToPosition)
                {
                    SnapToPositionValue(ref _KnobValue);
                    finalRotation.eulerAngles = new Vector3(0, 0, 360 - 360 * _KnobValue);
                }
            }
            else
            {
                _KnobValue = (finalRotation.eulerAngles.z / 360f);

                if (_SnapToPosition)
                {
                    SnapToPositionValue(ref _KnobValue);
                    finalRotation.eulerAngles = new Vector3(0, 0, 360 * _KnobValue);
                }
            }

            UpdateKnobValue();

            transform.rotation = finalRotation;
            InvokeEvents(_KnobValue + _currentLoops);

            _previousValue = _KnobValue;
        }

        private void UpdateKnobValue()
        {
            //PREVENT OVERROTATION
            if (Mathf.Abs(_KnobValue - _previousValue) > 0.5f)
            {
                if (_KnobValue < 0.5f && _Loops > 1 && _currentLoops < _Loops - 1)
                {
                    _currentLoops++;
                }
                else if (_KnobValue > 0.5f && _currentLoops >= 1)
                {
                    _currentLoops--;
                }
                else
                {
                    if (_KnobValue > 0.5f && _currentLoops == 0)
                    {
                        _KnobValue = 0;
                        transform.localEulerAngles = Vector3.zero;
                        InvokeEvents(_KnobValue + _currentLoops);
                        return;
                    }

                    if (_KnobValue < 0.5f && Mathf.Approximately(_currentLoops, _Loops - 1))
                    {
                        _KnobValue = 1;
                        transform.localEulerAngles = Vector3.zero;
                        InvokeEvents(_KnobValue + _currentLoops);
                        return;
                    }
                }
            }

            //CHECK MAX VALUE
            if (_MaxValue > 0 && _KnobValue + _currentLoops > _MaxValue)
            {
                _KnobValue = _MaxValue;
                var maxAngle = _Direction == Direction.Clockwise ? 360f - 360f * _MaxValue : 360f * _MaxValue;
                transform.localEulerAngles = new Vector3(0, 0, maxAngle);
                InvokeEvents(_KnobValue);
            }
        }

        public void SetKnobValue(float value, int loops = 0)
        {
            var newRotation = Quaternion.identity;
            _KnobValue = value;
            _currentLoops = loops;

            if (_SnapToPosition) 
                SnapToPositionValue(ref _KnobValue);
            
            newRotation.eulerAngles = _Direction == Direction.Clockwise ? new Vector3(0, 0, 360 - 360 * _KnobValue) : new Vector3(0, 0, 360 * _KnobValue);

            UpdateKnobValue();

            transform.rotation = newRotation;
            InvokeEvents(_KnobValue + _currentLoops);

            _previousValue = _KnobValue;
        }

        private void SnapToPositionValue(ref float knobValue)
        {
            var snapStep = 1 / (float)_SnapStepsPerLoop;
            var newValue = Mathf.Round(knobValue / snapStep) * snapStep;
            knobValue = newValue;
        }
        private void InvokeEvents(float value)
        {
            if (_ClampOutput01)
                value /= _Loops;
            _ValueChanged.Invoke(value);
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
