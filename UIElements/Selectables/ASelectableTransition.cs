// Author: František Holubec
// Created: 26.04.2024

using System;

namespace EDIVE.UIElements.Selectables
{
    [Serializable]
    public abstract class ASelectableTransition
    {
        public abstract void DoStateTransition(SelectionState state, bool instant = false);
    }
}
