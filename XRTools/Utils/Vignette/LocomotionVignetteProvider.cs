// Author: František Holubec
// Created: 08.07.2026

using System;
using System.Collections.Generic;
using EDIVE.OdinExtensions.Attributes;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

#if UNITY_EDITOR
using System.Collections;
#endif

namespace EDIVE.XRTools.Utils.Vignette
{
    [Serializable]
    public class LocomotionVignetteProvider : AConfiguredVignetteProvider
    {
        [SerializeField]
        [EnhancedValueDropdown(nameof(GetLocomotionProvidersDropdown), AppendNextDrawer = true, IsUniqueList = true, DrawDropdownForListElements = false)]
        private List<LocomotionProvider> _LocomotionProviders = new();

        protected override void OnInitialize()
        {
            foreach (var provider in _LocomotionProviders)
            {
                if (provider == null)
                    continue;
                provider.locomotionStarted += OnLocomotionStarted;
                provider.locomotionEnded += OnLocomotionEnded;
            }

            if (AnyLocomotionActive())
                Show();
        }

        protected override void OnDeinitialize()
        {
            foreach (var provider in _LocomotionProviders)
            {
                if (provider == null)
                    continue;
                provider.locomotionStarted -= OnLocomotionStarted;
                provider.locomotionEnded -= OnLocomotionEnded;
            }
        }

        private void OnLocomotionStarted(LocomotionProvider provider) => Show();

        private void OnLocomotionEnded(LocomotionProvider provider)
        {
            if (!AnyLocomotionActive())
                Hide();
        }

        private bool AnyLocomotionActive()
        {
            foreach (var provider in _LocomotionProviders)
            {
                if (provider != null && provider.isLocomotionActive)
                    return true;
            }
            return false;
        }

#if UNITY_EDITOR
        private IEnumerable GetLocomotionProvidersDropdown()
        {
            var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
                return stage.prefabContentsRoot.GetComponentsInChildren<LocomotionProvider>(true);
            return UnityEngine.Object.FindObjectsByType<LocomotionProvider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }
#endif
    }
}
