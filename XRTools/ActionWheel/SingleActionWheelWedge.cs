// Author: Michal Petr
// Created: 22.09.2026

using System.Collections.Generic;
using EDIVE.Localization;
using EDIVE.Utils.Actions;
using UnityEngine;
using UnityEngine.Localization;

namespace EDIVE.XRTools.ActionWheel
{
    public class SingleActionWheelWedge : AActionWheelWedge
    {
        [SerializeField]
        private SafeLocalizedString _Label;

        [SerializeReference]
        private List<IAction> _Actions = new();

        public override LocalizedString Label => _Label;

        public override void ExecuteActions(ActionWheelContext context)
        {
            _Actions.ForEach(action => action.Execute(context));
        }
    }
}
