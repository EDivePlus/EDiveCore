// Author: František Holubec
// Created: 08.10.2026

using TMPro;

namespace EDIVE.View.ViewTree
{
    [ViewAdapter(typeof(TMP_Text))]
    public class TMPTextViewAdapter : AComponentToggleViewAdapter<TMP_Text>
    {
        protected override void SetActive(TMP_Text component, bool active) => component.enabled = active;
    }
}
