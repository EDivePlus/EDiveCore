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
        [EnhancedBoxGroup("Enabled", "@ColorTools.Green")]
        private TweenAnimationField _EnableAnimation;

        [SerializeField]
        [HideLabel]
        [ListDrawerSettings(ShowFoldout = false)]
        [EnhancedBoxGroup("Disabled", "@ColorTools.Orange")]
        private TweenAnimationField _DisableAnimation;

        protected override void SetStateInternal(bool state, bool immediate = false)
        {
            GetAnim(!state)?.Kill();
            var anim = GetAnim(state);
            anim?.Play();
            if(immediate) anim?.Kill(true);
        }

        private ITweenAnimationPlayer GetAnim(bool state) => state ? _EnableAnimation : _DisableAnimation;
    }
}
