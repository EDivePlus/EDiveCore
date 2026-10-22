// Author: Michal Petr
// Created: 22.10.2026

using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.UIElements.Selectables;
using EDIVE.VisualPresets.Switchers;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EDIVE.XRTools.ScreenInteraction
{
    // Taps run the command, hold commands run while pressed
    public class ScreenInteractionCommandButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField]
        [Required]
        private EnhancedButton _Button;

        [SerializeField]
        private VisualSwitcher _Switcher = new();

        private ScreenInteractionCommandContext _context;
        private CancellationTokenSource _holdCts;
        private int _holdPointerId;

        public AScreenInteractionCommand Command { get; private set; }

        public void Setup(AScreenInteractionCommand command, ScreenInteractionCommandContext context)
        {
            if (Command != command)
                _Switcher.ApplyExclusive(command.Visual);

            Command = command;
            _context = context;
            _Button.interactable = command.IsUnlocked;
        }

        private void OnEnable() => _Button.onClick.AddListener(OnClicked);

        private void OnDisable()
        {
            _Button.onClick.RemoveListener(OnClicked);
            StopHold();
        }

        private void OnClicked()
        {
            if (Command is { IsHold: false })
                Command.Execute(_context, CancellationToken.None).Forget();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (Command is not { IsHold: true } || !_Button.IsInteractable() || _holdCts != null)
                return;

            _holdPointerId = eventData.pointerId;
            _holdCts = new CancellationTokenSource();
            Command.Execute(_context, _holdCts.Token).Forget();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == _holdPointerId)
                StopHold();
        }

        private void StopHold()
        {
            _holdCts?.Cancel();
            _holdCts?.Dispose();
            _holdCts = null;
        }
    }
}
