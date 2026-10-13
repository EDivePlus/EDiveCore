// Author: František Holubec
// Created: 13.10.2026

namespace EDIVE.Utils.ObjectActions
{
    // Replaced action, hidden from dropdowns. Validators offer the upgrade
    public interface ILegacyObjectAction
    {
        IObjectAction Upgrade();
    }
}
