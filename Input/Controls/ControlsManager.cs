using System.Collections.Generic;
using System.Linq;
using EDIVE.Core.Services;
using EDIVE.NativeUtils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Input.Controls
{
    public class ControlsManager : AServiceBehaviour<ControlsManager>
    {
        [SerializeField]
        private List<AControls> _Controls;
        
        private AControls _currentControls;

        private IEnumerable<AControls> AllControls => _Controls;

        protected void Awake()
        {
            _currentControls = SelectControls();
            AllControls.Where(c => c != null).ForEach(c => c.SetActive(false));
            if (_currentControls != null)
                _currentControls.SetActive(true);
        }

        private AControls SelectControls()
        {
            return _Controls.TryGetFirst(c => c != null && c.CheckAvailable(), out var controls) ? controls : null;
        }
        
        public void RequestTeleport(Vector3 position, Quaternion? rotation = null)
        {
            if (_currentControls)
            {
                _currentControls.RequestTeleport(position, rotation);
            }
        }
        
        [Button]
        public void TestRequestTeleport(Vector3 position, Quaternion rotation)
        {
            if (_currentControls)
            {
                _currentControls.RequestTeleport(position, rotation);
            }
        }
    }
}
