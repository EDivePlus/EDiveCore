using System;
using System.Collections.Generic;

namespace EDIVE.AppLoading
{
    public interface IDependencyOwner : ILoadInterface
    {
        IEnumerable<Type> GetDependencies();
    }
}
