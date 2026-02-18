using TMPro;
using UnityEngine;

namespace EDIVE.StagePlay.UI
{
    public class DirectionScriptSegmentDisplay : AScriptSegmentDisplay<DirectionScriptSegment>
    {
        [SerializeField]
        private TMP_Text _DescriptionText;
        
        protected override void SetData(SegmentDisplayData<DirectionScriptSegment> data)
        {
            base.SetData(data);
            if (_DescriptionText != null)
            {
                _DescriptionText.text = data.Segment.Description;
            }
        }
    }
}
