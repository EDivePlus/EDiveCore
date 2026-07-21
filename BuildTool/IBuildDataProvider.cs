// Author: František Holubec
// Created: 15.10.2025

using System.Collections.Generic;

namespace EDIVE.BuildTool
{
    public interface IBuildDataProvider
    {
        IEnumerable<string> GetBuildDefines(BuildContext context);
        IEnumerable<string> GetBuildScenes(BuildContext context);
        IEnumerable<IBuildCallback> GetBuildCallbacks(BuildContext context);
    }
}
