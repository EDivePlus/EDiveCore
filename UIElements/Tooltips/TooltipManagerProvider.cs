// Author: Michal Petr
// Created: 09.03.2026

using UnityEngine;

namespace EDIVE.UIElements.Tooltips
{
    public class TooltipManagerProvider : MonoBehaviour
    {
        [SerializeField]
        private TooltipManager _TooltipManager;
        public TooltipManager TooltipManager => _TooltipManager;
    }
}
