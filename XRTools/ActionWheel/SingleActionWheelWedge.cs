// Author: Michal Petr
// Created: 22.09.2026

using System.Collections.Generic;
using EDIVE.Utils.Actions;
using UnityEngine;

namespace EDIVE.XRTools.ActionWheel
{
    public class SingleActionWheelWedge : AActionWheelWedge
    {
        [SerializeReference]
        private List<IAction> _Actions = new();

        public override void ExecuteActions()
        {
            _Actions.ForEach(action => action.Execute());
        }
    }
}
