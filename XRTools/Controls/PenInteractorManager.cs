// Author: František Holubec
// Created: 02.10.2026

using System;
using System.Collections.Generic;
using EDIVE.Core.Services;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace EDIVE.XRTools.Controls
{
    public class PenInteractorManager : AServiceBehaviour<PenInteractorManager>
    {
        private const string PEN_INTERACTOR_PREF_KEY = "XR_PenInteractor";

        [SerializeField]
        private List<ControllerPenInteractor> _Controllers = new();

        public event Action<bool> PenInteractorEnabledChanged;

        [DisableInEditorMode]
        [ShowInInspector]
        public bool PenInteractorEnabled
        {
            get => PlayerPrefs.GetInt(PEN_INTERACTOR_PREF_KEY, 0) > 0;
            set
            {
                if (PenInteractorEnabled == value)
                    return;

                PlayerPrefs.SetInt(PEN_INTERACTOR_PREF_KEY, value ? 1 : 0);
                ApplyToControllers();
                PenInteractorEnabledChanged?.Invoke(value);
            }
        }

        private void Awake()
        {
            ApplyToControllers();
        }

        private void ApplyToControllers()
        {
            foreach (var controller in _Controllers)
                controller.Apply(PenInteractorEnabled);
        }

        [Serializable]
        private class ControllerPenInteractor
        {
            [SerializeField]
            private ControllerInputActionManager _Controller;

            [SerializeField]
            private NearFarInteractor _PenInteractor;

            public void Apply(bool enabled)
            {
                if (_Controller != null)
                    _Controller.RequestNearFarInteractor(enabled ? _PenInteractor : null);
            }
        }
    }
}
