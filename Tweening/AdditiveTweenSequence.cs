using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Tweening
{
    [Serializable]
    public class AdditiveTweenSequence : TweenSequence
    {
        [SerializeField]
        [InlineProperty]
        [HideLabel]
        private TweenAdditionOperation _Operation;

        public void ApplyTo(Sequence sequence)
        {
            sequence.ApplyOperation(CreateSequence(), _Operation);
        }
    }
}
