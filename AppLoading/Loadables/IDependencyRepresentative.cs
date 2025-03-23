using System;
using System.Collections.Generic;

namespace EDIVE.AppLoading.Loadables
{
    public interface IDependencyRepresentative : ILoadInterface
    {
        IEnumerable<Type> GetRepresentedTypes();
    }
}
