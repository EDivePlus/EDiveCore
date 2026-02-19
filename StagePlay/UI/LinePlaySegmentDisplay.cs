using TMPro;
using UnityEngine;

namespace EDIVE.StagePlay.UI
{
    public class LinePlaySegmentDisplay : APlaySegmentDisplay<SpeachPlaySegment>
    {
        [SerializeField]
        private TMP_Text _LineText;

        [SerializeField]
        private TMP_Text _CharactersText;
        
        protected override void SetData(SegmentDisplayData<SpeachPlaySegment> data)
        {
            base.SetData(data);
            
            if (_CharactersText != null) 
                _CharactersText.text = string.Join(", ", data.Segment.Characters);
            
            if (_LineText != null) 
                _LineText.text = data.Segment.Line;
        }
    }
}
