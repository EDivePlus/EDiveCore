// Author: Michal Petr
// Created: 25.05.2026

using EDIVE.OdinExtensions.Attributes;

namespace EDIVE.Localization.LocalizeStringModifiers
{
    [EnhancedTypeSelector(true, 1)]
    public interface ILocalizeStringModifier
    {
        string Apply(string input);
    }
}
