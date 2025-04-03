namespace EDIVE.Core.Services
{
    public abstract class AServiceBehaviour<T> : ABaseServiceBehaviour<T>
        where T : class, IService
    {
        protected virtual void Awake()
        {
            RegisterService();
        }

        protected virtual void OnDestroy()
        {
            UnregisterService();
        }
    }
}
