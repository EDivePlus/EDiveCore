using EDIVE.OdinExtensions.Attributes;
using EDIVE.StateHandling.ToggleStates;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Tweening.StateHandling
{
    public class TweenToggleState : AToggleState
    {
        [SerializeField]
        [HideLabel]
        [ListDrawerSettings(ShowFoldout = false)]
        [EnhancedBoxGroup("Enabled", "@FancyColors.Green")]
        private TweenAnimationField _EnableAnimation;

        [SerializeField]
        [HideLabel]
        [ListDrawerSettings(ShowFoldout = false)]
        [EnhancedBoxGroup("Disabled", "@FancyColors.Orange")]
        private TweenAnimationField _DisableAnimation;
        
        public override void UpdateState(bool immediate = false)
        {
            GetAnim(!_state)?.Kill();
            var anim = GetAnim(_state);
            anim?.Play();
            if(immediate) anim?.Kill(true);
        }

        private ITweenAnimationPlayer GetAnim(bool state) => state ? _EnableAnimation : _DisableAnimation;
    }
}
