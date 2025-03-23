using System;
using System.Collections.Generic;

namespace EDIVE.AppLoading.Loadables
{
    public interface IDependencyOwner : ILoadInterface
    {
        IEnumerable<Type> GetDependencies();
    }
}
