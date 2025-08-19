using TMPro;
using UnityEngine;

namespace EDIVE.StagePlay.UI
{
    public class LineScriptSegmentDisplay : AScriptSegmentDisplay<LineScriptSegment>
    {
        [SerializeField]
        private TMP_Text _LineText;

        [SerializeField]
        private TMP_Text _CharactersText;
        
        protected override void SetData(LineScriptSegment data)
        {
            base.SetData(data);
            
            if (_CharactersText != null) 
                _CharactersText.text = string.Join(", ", data.Characters);
        }
    }
}
