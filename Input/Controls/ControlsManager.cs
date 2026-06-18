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

        private const string HEIGHT_MODE_PREF_KEY = "XR_HeightMode";

        public static RigHeightMode SavedHeightMode
        {
            get => (RigHeightMode) PlayerPrefs.GetInt(HEIGHT_MODE_PREF_KEY, (int) RigHeightMode.Floor);
            set => PlayerPrefs.SetInt(HEIGHT_MODE_PREF_KEY, (int) value);
        }

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

        public void SetHeightMode(RigHeightMode mode)
        {
            SavedHeightMode = mode;
            if (CurrentControls)
            {
                CurrentControls.SetHeightMode(mode);
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
