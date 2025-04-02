using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading.Loadables;
using EDIVE.Core;
using EDIVE.Core.Services;

namespace EDIVE.AppLoading
{
    public abstract class ALoadableServiceBehaviour<T> : AServiceBehaviour<T>, ILoadable, IDependencyOwner
        where T : class, IService
    {
        protected virtual bool OnLoadedCalledManually => false;
        
        public async UniTask Load(Action<float> progressCallback)
        {
            await LoadRoutine(progressCallback);
            if (!OnLoadedCalledManually)
            {
                RegisterService();
            }
        }

        private void OnDestroy()
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
