// Author: Michal Petr
// Created: 03.03.2026

using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EDIVE.MenuScreen
{
    public class WidgetView : MonoBehaviour
    {
        public IViewContext CurrentContext { get; internal set; }
        public bool IsInitialized { get; private set; } = false;
        public MenuScreenController Controller { get; private set; }

        protected virtual UniTask InitializeInternal()
        {
            return UniTask.CompletedTask;
        }
        
        public async UniTask Initialize(MenuScreenController controller, IViewContext context = null)
        {
            if (IsInitialized)
                return;
            
            CurrentContext = context;
            Controller = controller;
            await InitializeInternal();
            IsInitialized = true;
            OnInitialize();
        }

        public virtual void OnInitialize() { }

        public virtual void OnOpen() { }
        public virtual void OnCollapse() { }
        public virtual void OnTerminate() { }
    }
}
