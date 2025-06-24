using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;

namespace EDIVE.StagePlay.Script.UI
{
    public class DirectionScriptSegmentDisplay : AScriptSegmentDisplay<DirectionScriptSegment>
    {
        [SerializeField]
        private LocalizeStringEvent _DescriptionText;

        protected override void SetData(DirectionScriptSegment data, StagePlayConfig config)
        {
            base.SetData(data, config);
            if (_DescriptionText != null)
                _DescriptionText.StringReference = data.DescriptionLocalized;
        }
    }
}