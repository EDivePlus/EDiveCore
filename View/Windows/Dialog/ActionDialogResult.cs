// Author: František Holubec
// Created: 09.10.2026

using System;
using EDIVE.Utils.Actions;
using UnityEngine;

namespace EDIVE.View.Windows.Dialog
{
    // DialogOpenButton runs it after close
    [Serializable]
    public class ActionDialogResult : IDialogResult
    {
        [SerializeReference]
        private IAction _Action;

        public object Value => _Action;
    }
}
