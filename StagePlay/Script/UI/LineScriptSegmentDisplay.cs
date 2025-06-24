using EDIVE.Localization;
using UnityEngine;
using UnityEngine.Localization.Components;

namespace EDIVE.StagePlay.Script.UI
{
    public class LineScriptSegmentDisplay : AScriptSegmentDisplay<LineScriptSegment>
    {
        [SerializeField]
        private LocalizeStringEvent _MainLineText;

        [SerializeField]
        private LocalizeStringEvent _SecondaryLineText;

        [SerializeField]
        private LocalizeStringEvent _LocalizedLineText;

        [SerializeField]
        private LocalizeStringEvent _CharacterNameText;

        protected override void SetData(LineScriptSegment data, StagePlayConfig config)
        {
            base.SetData(data, config);
            if (_MainLineText != null)
                _MainLineText.StringReference = data.LineLocalized.WithLocaleOverride(config.MainTextLocale);

            if (_SecondaryLineText != null)
                _SecondaryLineText.StringReference = data.LineLocalized.WithLocaleOverride(config.SecondaryTextLocale);

            if (_LocalizedLineText != null)
                _LocalizedLineText.StringReference = data.LineLocalized;

            if (_CharacterNameText != null && data.Speaker != null)
                _CharacterNameText.StringReference = data.Speaker.NameLocalized;
        }
    }
}