// Author: František Holubec
// Created: 09.10.2026

using System;
using UnityEngine;

namespace EDIVE.View.Windows.Dialog
{
    [Serializable]
    public class BoolDialogResult : IDialogResult
    {
        [SerializeField]
        private bool _Value;

        public object Value => _Value;
    }
}
