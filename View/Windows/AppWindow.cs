// Author: František Holubec
// Created: 14.10.2026

using Cysharp.Threading.Tasks;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.Utils.Activations;
using EDIVE.VisualPresets.Switchers;
using UnityEngine;
using UnityEngine.Serialization;

namespace EDIVE.View.Windows
{
    // Window of an app, the header shows the definition visual
    public class AppWindow : AWindow
    {
        [SerializeField]
        [EnhancedBoxGroup("App")]
        [FormerlySerializedAs("_Switcher")]
        private VisualSwitcher _VisualSwitcher = new();

        [Tooltip("E.g. the header close button, asks CanCloseAsync first")]
        [SerializeReference]
        [EnhancedBoxGroup("App")]
        private IActivation _CloseActivation;

        [SerializeReference]
        [EnhancedBoxGroup("App")]
        private IActivation _MinimizeActivation;

        private readonly DisposableGroup _listeners = new();

        protected override void OnSetup() => _VisualSwitcher.Apply(Definition.Visual);

        protected override void OnEnable()
        {
            base.OnEnable();
            _listeners.Add(_CloseActivation.RegisterDisposable(OnCloseActivated));
            _listeners.Add(_MinimizeActivation.RegisterDisposable(Minimize));
        }

        protected override void OnDisable()
        {
            _listeners.Clear();
            base.OnDisable();
        }

        private void OnCloseActivated()
        {
            if (FocusedInTree)
                TryCloseAsync().Forget();
        }
    }
}
