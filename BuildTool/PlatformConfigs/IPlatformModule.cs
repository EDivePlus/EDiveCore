// Author: František Holubec
// Created: 19.03.2026

namespace EDIVE.BuildTool.PlatformConfigs
{
    public interface IPlatformModule
    {
        void SetupBeforeBuild(BuildContext context);
        void RestoreAfterBuild(BuildContext context);
    }
}
