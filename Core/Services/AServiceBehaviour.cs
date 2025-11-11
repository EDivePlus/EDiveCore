namespace EDIVE.Core.Services
{
    public abstract class AServiceBehaviour<T> : ABaseServiceBehaviour<T>
        where T : class, IService
    {
        protected virtual void OnEnable()
        {
            RegisterService();
        }

        protected virtual void OnDisable()
        {
            UnregisterService();
        }
    }
}
