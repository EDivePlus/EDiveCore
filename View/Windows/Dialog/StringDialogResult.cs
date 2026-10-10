// Author: František Holubec
// Created: 09.10.2026

using System;
using UnityEngine;

namespace EDIVE.View.Windows.Dialog
{
    [Serializable]
    public class StringDialogResult : IDialogResult
    {
        [SerializeField]
        private string _Value;

        public object Value => _Value;
    }
}
