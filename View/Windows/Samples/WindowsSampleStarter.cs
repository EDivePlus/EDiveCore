// Author: František Holubec
// Created: 09.10.2026

using UnityEngine;

namespace EDIVE.View.Windows.Samples
{
    // Opens the first frame on start
    public class WindowsSampleStarter : MonoBehaviour
    {
        [SerializeField]
        private ScreenRoot _Screen;

        [SerializeField]
        private WindowDefinition _FirstWindow;

        private void Start() => _Screen.Open(_FirstWindow, "Main Frame");
    }
}
