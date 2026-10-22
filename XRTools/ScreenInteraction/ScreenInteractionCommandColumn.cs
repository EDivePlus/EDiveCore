// Author: Michal Petr
// Created: 22.10.2026

using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.NativeUtils;
using EDIVE.StateHandling.ToggleStates;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.XRTools.ScreenInteraction
{
    // Buttons for the commands of the targeted or the held interactable
    public class ScreenInteractionCommandColumn : MonoBehaviour
    {
        [SerializeField]
        [Required]
        private ScreenInteractionController _Controller;

        [Tooltip("Shows the held interactable, otherwise the target")]
        [SerializeField]
        private bool _ForHeld;

        [SerializeField]
        [Required]
        private ScreenInteractionCommandButton _ButtonPrefab;

        [SerializeField]
        [Required]
        private RectTransform _Container;

        [Tooltip("On while the column has buttons")]
        [SerializeField]
        private AToggleState _VisibleState;

        private readonly List<ScreenInteractionCommandButton> _buttons = new();
        private readonly List<AScreenInteractionCommand> _wanted = new();
        private ScreenInteractable _interactable;
        private IDisposable _observer;
        private bool _dirty;

        private void OnEnable()
        {
            _Controller.Changed += OnControllerChanged;
            OnControllerChanged();
        }

        private void OnDisable()
        {
            _Controller.Changed -= OnControllerChanged;
            Observe(null);
        }

        // Batched, conditions can report several changes in one frame
        private void LateUpdate()
        {
            if (_dirty)
                Rebuild();
        }

        private void OnControllerChanged()
        {
            var interactable = _ForHeld ? _Controller.Held : _Controller.Target;
            if (interactable != _interactable)
                Observe(interactable);
            _dirty = true;
        }

        private void Observe(ScreenInteractable interactable)
        {
            _observer?.Dispose();
            _interactable = interactable;
            if (interactable == null)
            {
                _observer = null;
                return;
            }

            var observers = interactable.Commands.Where(command => command != null).Select(command => command.Observe(MarkDirty)).ToList();
            _observer = DisposableUtils.Create(() => observers.ForEach(observer => observer.Dispose()));
        }

        private void MarkDirty() => _dirty = true;

        private void Rebuild()
        {
            _dirty = false;
            var context = new ScreenInteractionCommandContext(_Controller, _ForHeld);

            _wanted.Clear();
            if (_interactable != null)
                _wanted.AddRange(_interactable.Commands.Where(command => command != null && command.IsShown(context)));

            // Keeps buttons that stay, so a running hold isn't cut
            for (var i = _buttons.Count - 1; i >= 0; i--)
            {
                if (_buttons[i] != null && _wanted.Contains(_buttons[i].Command))
                    continue;

                if (_buttons[i] != null)
                    Destroy(_buttons[i].gameObject);
                _buttons.RemoveAt(i);
            }

            for (var i = 0; i < _wanted.Count; i++)
            {
                var command = _wanted[i];
                var button = _buttons.Find(b => b.Command == command);
                if (button == null)
                {
                    button = Instantiate(_ButtonPrefab, _Container, false);
                    _buttons.Add(button);
                }
                button.Setup(command, context);
                button.transform.SetSiblingIndex(i);
            }

            if (_VisibleState != null)
                _VisibleState.SetState(_wanted.Count > 0);
        }
    }
}
