using System;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace EDIVE.Localization
{
    [Serializable]
    // Interface listed again so Localization calls this instead of the base method
    public class FilteredSystemLocaleSelector : SystemLocaleSelector, IStartupLocaleSelector
    {
        public new Locale GetStartupLocale(ILocalesProvider availableLocales)
        {
            var baseLocale = base.GetStartupLocale(availableLocales);
            if (baseLocale == null)
                return null;

            var ignored = baseLocale.Metadata.GetMetadata<IgnoreLocale>();
            return ignored == null ? baseLocale : null;
        }
    }
}
