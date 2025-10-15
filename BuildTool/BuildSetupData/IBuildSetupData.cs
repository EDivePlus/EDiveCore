// Author: František Holubec
// Created: 15.10.2025

using System.Collections.Generic;
using EDIVE.BuildTool.Actions;

namespace EDIVE.BuildTool.BuildSetupData
{
    public interface IBuildSetupData
    {
        IEnumerable<string> Defines { get; }
        IEnumerable<IBuildAction> Actions { get; }
    }
}
