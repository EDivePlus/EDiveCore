// Author: František Holubec
// Created: 05.06.2025

using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.Utils
{
    [RequireComponent(typeof(Button))]
    public class SRDebuggerOpenButton : MonoBehaviour
    {
        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_button)
            {
                _button.onClick.AddListener(OnButtonClicked);
            }
        }

        private void OnButtonClicked()
        {
#if SR_DEBUGGER
            SRDebug.Instance?.ShowDebugPanel();
#endif
        }
    }
}
