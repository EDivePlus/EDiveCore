namespace EDIVE.Core.Services
{
    public abstract class ServiceBehaviour<T> : AServiceBehaviour<T> 
        where T : class, IService
    {
        private void Awake()
        {
            RegisterService();
        }

        private void OnDestroy()
        {
            UnregisterService();
        }
    }
}
