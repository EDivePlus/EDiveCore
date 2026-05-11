// Author: František Holubec
// Created: 2026-05-07

using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace EDIVE.Input.Touch
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct TouchControlsState : IInputStateTypeInfo
    {
        public FourCC format => new('T', 'C', 'H', 'C');

        [InputControl(layout = "Stick", displayName = "Left Stick", shortDisplayName = "LS")]
        [FieldOffset(0)] public Vector2 leftStick;

        [InputControl(layout = "Stick", displayName = "Right Stick", shortDisplayName = "RS")]
        [FieldOffset(8)] public Vector2 rightStick;
    }

    [InputControlLayout(stateType = typeof(TouchControlsState), displayName = "Touch Controls")]
    public class TouchControls : InputDevice
    {
        public StickControl LeftStick  { get; private set; }
        public StickControl RightStick { get; private set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();
            LeftStick  = GetChildControl<StickControl>("leftStick");
            RightStick = GetChildControl<StickControl>("rightStick");
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void InitEditor() => Register();
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register() => InputSystem.RegisterLayout<TouchControls>();
    }
}
