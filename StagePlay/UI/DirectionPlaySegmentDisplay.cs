using TMPro;
using UnityEngine;

namespace EDIVE.StagePlay.UI
{
    public class DirectionPlaySegmentDisplay : APlaySegmentDisplay<DirectionPlaySegment>
    {
        [SerializeField]
        private TMP_Text _DescriptionText;
        
        protected override void SetData(SegmentDisplayData<DirectionPlaySegment> data)
        {
            base.SetData(data);
            if (_DescriptionText != null)
            {
                _DescriptionText.text = data.Segment.Description;
            }
        }
    }
}
