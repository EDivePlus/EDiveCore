// Author: František Holubec
// Created: 15.10.2026

using EDIVE.StateHandling.ToggleStates;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.View.Windows.Screens
{
    // On while a window is open on the layer, e.g. the dock turns as solid as the windows
    public class ScreenWindowsState : MonoBehaviour
    {
        [SerializeField]
        [Required]
        private ScreenRoot _Screen;

        [SerializeField]
        [Required]
        private WindowLayer _Layer;

        [SerializeField]
        [Required]
        private AToggleState _OpenState;

        private void OnEnable()
        {
            _Screen.Changed += OnChanged;
            Refresh(true);
        }

        private void OnDisable()
        {
            _Screen.Changed -= OnChanged;
        }

        private void OnChanged(ScreenRoot screen) => Refresh(false);

        private void Refresh(bool immediate) => _OpenState.SetState(_Screen.GetFront(_Layer) != null, immediate);
    }
}
