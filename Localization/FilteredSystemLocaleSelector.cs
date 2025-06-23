using System;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace EDIVE.Localization
{
    [Serializable]
    public class FilteredSystemLocaleSelector : SystemLocaleSelector
    {
        public new Locale GetStartupLocale(ILocalesProvider availableLocales)
        {
            var baseLocale = base.GetStartupLocale(availableLocales);
            var ignored = baseLocale.Metadata.GetMetadata<IgnoreLocale>();
            return ignored == null ? baseLocale : null;
        }
    }
}
