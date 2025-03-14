using Cysharp.Threading.Tasks;

namespace EDIVE.AppLoading
{
    public interface ILoadFinalizer : ILoadInterface
    {
        public UniTask<bool> TryFinalizeLoad();
    }
}
