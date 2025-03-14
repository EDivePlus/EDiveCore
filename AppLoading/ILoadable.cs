using System;
using Cysharp.Threading.Tasks;

namespace EDIVE.AppLoading
{
    public interface ILoadable : ILoadInterface
    {
        UniTask Load(Action<float> progressCallback);
    }
}
