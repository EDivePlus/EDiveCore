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

        public AControls CurrentControls { get; private set; }

        private IEnumerable<AControls> AllControls => _Controls;

        protected void Awake()
        {
            CurrentControls = SelectControls();
            AllControls.Where(c => c != null).ForEach(c => c.SetActive(false));
            if (CurrentControls != null)
                CurrentControls.SetActive(true);
        }

        private AControls SelectControls()
        {
            return _Controls.TryGetFirst(c => c != null && c.CheckAvailable(), out var controls) ? controls : null;
        }
        
        public void RequestTeleport(Vector3 position, Quaternion? rotation = null)
        {
            if (CurrentControls)
            {
                CurrentControls.RequestTeleport(position, rotation);
            }
        }
        
        [Button]
        public void TestRequestTeleport(Vector3 position, Quaternion rotation)
        {
            if (CurrentControls)
            {
                CurrentControls.RequestTeleport(position, rotation);
            }
        }
    }
}
