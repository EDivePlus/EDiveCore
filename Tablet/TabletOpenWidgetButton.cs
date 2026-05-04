// Author: Michal Petr
// Created: 04.05.2026

using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.Tablet
{
    public class TabletOpenWidgetButton : MonoBehaviour
    {
        [SerializeField]
        [Required]
        private Button _OpenButton;

        [SerializeField]
        [Required]
        private TabletWidgetDefinition _WidgetToOpen;

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

            var parentView = GetComponentInParent<TabletWidgetView>();
            if (parentView == null)
            {
                Debug.LogWarning($"{nameof(TabletOpenWidgetButton)} requires a parent {nameof(TabletWidgetView)}.", this);
                return;
            }

            var controller = parentView.Context?.Controller;
            if (controller == null)
            {
                Debug.LogWarning($"Parent {nameof(TabletWidgetView)} has no controller in context.", this);
                return;
            }

            if (!controller.Definition.Widgets.Contains(_WidgetToOpen))
            {
                Debug.LogWarning($"Widget '{_WidgetToOpen.name}' is not in the tablet's widget definitions.", this);
                return;
            }

            controller.OpenWidget(_WidgetToOpen);
        }
    }
}
