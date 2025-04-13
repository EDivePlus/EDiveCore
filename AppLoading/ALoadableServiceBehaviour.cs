using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading.Loadables;
using EDIVE.Core.Services;

namespace EDIVE.AppLoading
{
    public abstract class ALoadableServiceBehaviour<T> : ABaseServiceBehaviour<T>, ILoadable, IDependencyOwner
        where T : class, IService
    {
        protected virtual bool RegisterServiceOnLoad => true;
        
        public async UniTask Load(Action<float> progressCallback)
        {
            await LoadRoutine(progressCallback);
            if (RegisterServiceOnLoad)
            {
                RegisterService();
            }
        }

        protected virtual void OnDestroy()
        {
            UnregisterService();
        }

        protected abstract UniTask LoadRoutine(Action<float> progressCallback);

        public IEnumerable<Type> GetDependencies()
        {
            var dependencies = new HashSet<Type>();
            PopulateDependencies(dependencies);
            return dependencies;
        }

        protected virtual void PopulateDependencies(HashSet<Type> dependencies)
        {

        }
    }
}
