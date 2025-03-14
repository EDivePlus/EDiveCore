using System;
using System.Collections.Generic;

namespace EDIVE.AppLoading
{
    public interface IDependencyRepresentative : ILoadInterface
    {
        IEnumerable<Type> GetRepresentedTypes();
    }
}
