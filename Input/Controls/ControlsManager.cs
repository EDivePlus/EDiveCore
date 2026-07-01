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
        [ListDrawerSettings(ShowFoldout = false)]
        private List<AControls> _Controls;

        public AControls CurrentControls { get; private set; }

        private IEnumerable<AControls> AllControls => _Controls;

        private const string HEIGHT_MODE_PREF_KEY = "Controls_HeightMode";
        
        [PropertySpace]
        [SerializeField]
        private RigHeightMode _DefaultHeightMode = RigHeightMode.Automatic;
        
        [EnumToggleButtons]
        [ShowInInspector]
        public RigHeightMode CurrentHeightMode
        {
            get => (RigHeightMode) PlayerPrefs.GetInt(HEIGHT_MODE_PREF_KEY, (int) _DefaultHeightMode);
            set => SetHeightMode(value);
        }

        protected void Awake()
        {
            CurrentControls = SelectControls();
            AllControls.Where(c => c != null).ForEach(c => c.SetActive(false));
            if (CurrentControls != null)
            {
                CurrentControls.SetActive(true);
                CurrentControls.SetHeightMode(CurrentHeightMode);
            }
        }

        private AControls SelectControls()
        {
            return _Controls.TryGetFirst(c => c != null && c.CheckAvailable(), out var controls) ? controls : null;
        }
        
        public void RequestTeleport(Vector3 position, Quaternion? rotation = null)
        {
            if (CurrentControls) 
                CurrentControls.RequestTeleport(position, rotation);
        }

        public void SetHeightMode(RigHeightMode mode)
        {
            PlayerPrefs.SetInt(HEIGHT_MODE_PREF_KEY, (int) mode);
            if (CurrentControls) 
                CurrentControls.SetHeightMode(mode);
        }
        
        [Button]
        public void TestRequestTeleport(Vector3 position, Quaternion rotation)
        {
            if (CurrentControls) 
                CurrentControls.RequestTeleport(position, rotation);
        }
    }
}
