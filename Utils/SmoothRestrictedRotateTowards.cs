using EDIVE.NativeUtils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace EDIVE.Utils
{
    [ExecuteAlways]
    public class SmoothRestrictedRotateTowards : MonoBehaviour
    {
        [FormerlySerializedAs("controlledObject")]
        [SerializeField]
        public Transform _ControlledObject;

        [FormerlySerializedAs("localTarget")]
        [SerializeField]
        [LabelText("Target")]
        private GameObject _LocalTarget;
        
        [FormerlySerializedAs("smoothTime")]
        [PropertySpace]
        [SerializeField]
        private float _SmoothTime = 0.05f;

        [FormerlySerializedAs("xRange")]
        [SerializeField]
        [MinMaxSlider(-1, 1, true)]
        private Vector2 _XRange = new(-1,1);
        
        [FormerlySerializedAs("yRange")]
        [SerializeField]
        [MinMaxSlider(-1, 1, true)]
        private Vector2 _YRange = new(-1,1);
        
        [FormerlySerializedAs("zRange")]
        [SerializeField]
        [MinMaxSlider(-1, 1, true)]
        private Vector2 _ZRange = new(-1,1);
        
        private Vector3 _smoothingVelocity = Vector3.zero;

        public GameObject Target => _LocalTarget;

        private void LateUpdate()
        {
            if (!Target)
                return;

            var direction = (Target.transform.position - _ControlledObject.position).normalized.ClampRange(_XRange, _YRange, _ZRange);
            var smoothedDirection = Vector3.SmoothDamp(_ControlledObject.forward, direction, ref _smoothingVelocity, _SmoothTime);
            _ControlledObject.forward = smoothedDirection;
        }
    }
}
