// Author: František Holubec
// Created: 05.05.2026

using UnityEngine;

namespace EDIVE.XRTools.Controls.Mobile
{
    // Adapter so MobileCameraController stays decoupled from any specific joystick plugin.
    // Subclass and forward Value from whichever asset-store joystick is in use.
    public abstract class AJoystickProvider : MonoBehaviour
    {
        public abstract Vector2 Value { get; }
    }
}
