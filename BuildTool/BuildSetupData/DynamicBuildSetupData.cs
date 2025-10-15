// Author: František Holubec
// Created: 15.10.2025

using System.Collections.Generic;
using System.Linq;
using EDIVE.BuildTool.Actions;

namespace EDIVE.BuildTool.BuildSetupData
{
    public class DynamicBuildSetupData : IBuildSetupData
    {
        private readonly List<string> _defines;
        private readonly List<IBuildAction> _actions;
        
        public IEnumerable<string> Defines => _defines ?? Enumerable.Empty<string>();
        public IEnumerable<IBuildAction> Actions => _actions ?? Enumerable.Empty<IBuildAction>();
        
        public DynamicBuildSetupData(List<string> defines, List<IBuildAction> actions)
        {
            _defines = defines;
            _actions = actions;
        }
        
        public DynamicBuildSetupData(List<IBuildAction> actions)
        {
            _actions = actions;
        }
        
        public DynamicBuildSetupData(params IBuildAction[] actions)
        {
            _actions = actions.ToList();
        }
    }
}
