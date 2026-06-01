// Author: Michal Petr
// Created: 04.05.2026

using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.MenuScreen
{
    public class OpenWidgetButton : MonoBehaviour
    {
        [SerializeField]
        [Required]
        private Button _OpenButton;

        [SerializeField]
        [Required]
        private WidgetDefinition _WidgetToOpen;

        private void Awake()
        {
            _OpenButton.onClick.AddListener(OnOpenButtonClicked);
        }

        private void OnDestroy()
        {
            if (_OpenButton)
                _OpenButton.onClick.RemoveListener(OnOpenButtonClicked);
        }

        private void OnOpenButtonClicked()
        {
            if (_WidgetToOpen == null)
                return;

            var parentView = GetComponentInParent<WidgetView>();
            if (parentView == null)
            {
                Debug.LogWarning($"{nameof(OpenWidgetButton)} requires a parent {nameof(WidgetView)}.", this);
                return;
            }

            var controller = parentView.Controller;
            if (controller == null)
            {
                Debug.LogWarning($"Parent {nameof(WidgetView)} has no controller in context.", this);
                return;
            }

            if (!controller.Definition.Widgets.Contains(_WidgetToOpen))
            {
                Debug.LogWarning($"Widget '{_WidgetToOpen.name}' is not in the MenuScreen's widget definitions.", this);
                return;
            }

            controller.OpenWidget(_WidgetToOpen);
        }
    }
}
