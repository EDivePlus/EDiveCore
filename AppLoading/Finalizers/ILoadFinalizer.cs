using Cysharp.Threading.Tasks;
using EDIVE.AppLoading.Loadables;

namespace EDIVE.AppLoading.Finalizers
{
    public interface ILoadFinalizer : ILoadInterface
    {
        public UniTask<bool> TryFinalizeLoad();
    }
}
