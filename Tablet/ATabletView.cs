// Author: Michal Petr
// Created: 03.03.2026

using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EDIVE.Tablet
{
    public interface ITabletViewContext { }
    
    public abstract class ATabletView : MonoBehaviour
    {
        public ITabletViewContext Context { get; private set; }
        public bool IsInitialized { get; private set; } = false;
        protected TabletController Controller { get; private set; }

        protected virtual UniTask InitializeInternal()
        {
            return UniTask.CompletedTask;
        }
        
        public async UniTask Initialize(TabletController controller, ITabletViewContext context = null)
        {
            if (IsInitialized)
                return;
            
            Context = context;
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

    public abstract class ATabletView<TContext> : ATabletView where TContext : ITabletViewContext
    {
        public new TContext Context => (TContext) base.Context;

        public async UniTask Initialize(TabletController controller, TContext context) => await base.Initialize(controller, context);
    }
}
