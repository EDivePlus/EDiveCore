using UnityEngine;

namespace EDIVE.Core.Services
{
    public abstract class ABaseServiceBehaviour<T> : MonoBehaviour, IService
        where T : class, IService
    {
        protected void RegisterService<TService>() where TService : class, IService
        {
            if (this is not TService targetType)
            {
                Debug.LogError($"Service '{GetType()}' cannot be registered as '{typeof(TService)}'", this);
                return;
            }
            AppCore.Services.Register(targetType);
        }

        protected void UnregisterService<TService>() where TService : class, IService
        {
            if (this is not TService)
            {
                Debug.LogError($"Service '{GetType()}' cannot be unregistered as '{typeof(TService)}'", this);
                return;
            }
            AppCore.Services.Unregister<TService>();
        }

        protected virtual void RegisterService() => RegisterService<T>();
        protected virtual void UnregisterService() => UnregisterService<T>();
    }
}
